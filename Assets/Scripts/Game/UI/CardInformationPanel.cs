using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XHHY.Game.Data;
using XHHY.Game.Runtime;

namespace XHHY.Game.UI
{
    /// <summary>
    /// 场景 CardInformation：点击手牌打开详情；人物可喂食与装备。
    /// </summary>
    public class CardInformationPanel : MonoBehaviour
    {
        public static CardInformationPanel Instance { get; private set; }

        [SerializeField] GameObject root;
        [SerializeField] Image portrait;
        [SerializeField] TMP_Text titleText;
        [SerializeField] TMP_Text typeText;
        [SerializeField] TMP_Text descText;
        [SerializeField] TMP_Text attrsText;
        [SerializeField] Button closeButton;
        [SerializeField] RectTransform equipSlotContainer;
        [SerializeField] EventCardSlot equipSlotPrefab;
        [SerializeField] EventCardSlot eatSlot;
        [SerializeField] HandTrayController handTray;

        HandCardView _target;
        readonly List<EventCardSlot> _equipSlots = new List<EventCardSlot>();

        void Awake()
        {
            Instance = this;
            if (root == null)
                root = gameObject;
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }
            Hide();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public static void Open(HandCardView card)
        {
            if (Instance == null)
                Instance = Object.FindFirstObjectByType<CardInformationPanel>(FindObjectsInactive.Include);
            Instance?.Show(card);
        }

        public void Show(HandCardView card)
        {
            if (card?.Data == null)
                return;
            _target = card;
            if (root != null)
                root.SetActive(true);
            gameObject.SetActive(true);

            if (portrait != null)
                portrait.sprite = card.Data.icon;
            if (titleText != null)
                titleText.text = card.Data.title;
            if (typeText != null)
                typeText.text = card.Data.cardType != null ? card.Data.cardType.displayName : string.Empty;
            if (descText != null)
                descText.text = card.Data.description;

            RefreshAttrs();
            RebuildCharacterSlots();
        }

        void RefreshAttrs()
        {
            if (attrsText == null || _target?.Data == null)
                return;
            var inst = _target.Instance;
            var sb = new System.Text.StringBuilder();
            var attrs = _target.Data.GetSortedAttributes();
            for (int i = 0; i < attrs.Count; i++)
            {
                var a = attrs[i];
                int bonus = inst != null ? inst.GetModifier(a.attributeId) : 0;
                if (inst != null)
                {
                    int effective = inst.GetEffectiveAttribute(a.attributeId, uid => GameState.Ensure().FindByUid(uid));
                    bonus = effective - a.value;
                }
                if (bonus != 0)
                    sb.AppendLine($"{a.attributeId} {a.value} <color=#3DDC97>+{bonus}</color>");
                else
                    sb.AppendLine($"{a.attributeId} {a.value}");
            }
            attrsText.text = sb.ToString();
        }

        void RebuildCharacterSlots()
        {
            for (int i = 0; i < _equipSlots.Count; i++)
            {
                if (_equipSlots[i] != null)
                    Destroy(_equipSlots[i].gameObject);
            }
            _equipSlots.Clear();

            bool isCharacter = _target?.Data?.cardType != null && _target.Data.cardType.typeId == "renwu";
            if (eatSlot != null)
                eatSlot.gameObject.SetActive(isCharacter);

            if (!isCharacter || equipSlotContainer == null || equipSlotPrefab == null)
                return;

            var slots = _target.Data.equipSlots;
            if (slots == null || slots.Count == 0)
                slots = new List<string> { "weapon", "armor", "accessory" };

            for (int i = 0; i < slots.Count; i++)
            {
                var slot = Instantiate(equipSlotPrefab, equipSlotContainer);
                slot.gameObject.SetActive(true);
                var data = new EventSlotData
                {
                    slotId = slots[i],
                    required = false,
                    anchoredPosition = new Vector2(i * 120f, 0f)
                };
                // 装备槽接受 zhuangbei / gongju
                slot.Setup(data);
                slot.CardPlaced += OnEquipPlaced;
                _equipSlots.Add(slot);
            }

            if (eatSlot != null)
            {
                eatSlot.CardPlaced -= OnEatPlaced;
                eatSlot.CardPlaced += OnEatPlaced;
                eatSlot.Setup(new EventSlotData
                {
                    slotId = "eat",
                    required = false,
                    acceptGroup = "food"
                });
            }
        }

        void OnEquipPlaced(EventCardSlot slot, HandCardView card)
        {
            if (_target?.Instance == null || card?.Instance == null)
                return;
            var state = GameState.Ensure();
            card.Instance.equippedOnUid = _target.Instance.uid;
            state.MoveCard(card.Instance, CardLocation.Equipped);
            if (!_target.Instance.equippedItemUids.Contains(card.Instance.uid))
                _target.Instance.equippedItemUids.Add(card.Instance.uid);
            _target.RefreshVisual();
            RefreshAttrs();
        }

        void OnEatPlaced(EventCardSlot slot, HandCardView card)
        {
            if (_target?.Instance == null || card?.Data == null)
                return;
            var state = GameState.Ensure();
            if (card.Data.onEat != null)
            {
                for (int i = 0; i < card.Data.onEat.Count; i++)
                {
                    var m = card.Data.onEat[i];
                    if (m != null)
                        _target.Instance.AddModifier(m.attributeId, m.value);
                }
            }

            if (card.Instance != null && card.Instance.data != null && card.Instance.data.IsStackable)
                state.ConsumeFromStack(card.Instance, 1);
            else if (card.Instance != null)
                state.MoveCard(card.Instance, CardLocation.Consumed);

            slot.Clear();
            Destroy(card.gameObject);
            handTray?.RebuildViewsFromState();
            RefreshAttrs();
            _target.RefreshVisual();
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }
    }
}
