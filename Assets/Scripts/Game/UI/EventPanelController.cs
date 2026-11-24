using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XHHY.Game.Data;

namespace XHHY.Game.UI
{
    public class EventPanelController : MonoBehaviour
    {
        public static EventPanelController Instance { get; private set; }

        [Header("根与遮罩")]
        [SerializeField] GameObject panelRoot;
        [SerializeField] GameObject dimmer;

        [Header("事件内容")]
        [SerializeField] Image eventBg;
        [SerializeField] TMP_Text eventNameText;
        [SerializeField] EventNarrativeView narrativeView;
        [SerializeField] EventAttributeView attributeView;
        [SerializeField] RectTransform cardSlotPanel;
        [SerializeField] EventCardSlot cardSlotPrefab;

        [Header("操作")]
        [SerializeField] Button confirmButton;
        [SerializeField] Button cancelButton;

        [Header("关联")]
        [SerializeField] HandTrayController handTray;

        readonly List<EventCardSlot> _spawnedSlots = new List<EventCardSlot>();
        EventData _current;
        bool _committed;

        public void Wire(
            GameObject root,
            GameObject dimmerGo,
            Image bg,
            TMP_Text nameText,
            EventNarrativeView narrative,
            EventAttributeView attribute,
            RectTransform slotPanel,
            EventCardSlot slotPrefab,
            Button confirm,
            Button cancel,
            HandTrayController tray)
        {
            panelRoot = root != null ? root : gameObject;
            dimmer = dimmerGo;
            eventBg = bg;
            eventNameText = nameText;
            narrativeView = narrative;
            attributeView = attribute;
            cardSlotPanel = slotPanel;
            cardSlotPrefab = slotPrefab;
            confirmButton = confirm;
            cancelButton = cancel;
            handTray = tray;
        }

        void Awake()
        {
            Instance = this;
            if (panelRoot == null)
                panelRoot = gameObject;

            if (confirmButton != null)
            {
                confirmButton.onClick.RemoveListener(Confirm);
                confirmButton.onClick.AddListener(Confirm);
            }

            if (cancelButton != null)
            {
                cancelButton.onClick.RemoveListener(Cancel);
                cancelButton.onClick.AddListener(Cancel);
            }

            HideImmediate();
        }

        void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }

        public void Open(EventData data)
        {
            if (data == null)
                return;

            _current = data;
            _committed = false;
            panelRoot.SetActive(true);
            if (dimmer != null)
                dimmer.SetActive(true);

            if (eventBg != null)
                eventBg.sprite = data.background;

            if (eventNameText != null)
                eventNameText.text = data.title ?? string.Empty;

            narrativeView?.SetOpening(data.openingText);
            RebuildSlots(data);
            RefreshAttributesAndPreview();

            // 手牌始终在主界面可用，并提到 Canvas 最前，避免被 Dimmer 挡住
            if (handTray != null)
            {
                handTray.gameObject.SetActive(true);
                handTray.BringToFront();
                handTray.ClearTypeFilter();
                handTray.Relayout();
            }
        }

        public void Confirm()
        {
            if (_current == null)
                return;

            if (!HasAllRequiredSlotsFilled())
            {
                Debug.Log("还有必填槽位未放入卡牌。");
                return;
            }

            // 确认：卡留在槽里，面板关闭，等待后续结算
            _committed = true;
            handTray?.ClearTypeFilter();
            HideImmediate();
        }

        public void Cancel()
        {
            ReturnAllSlotCardsToHand();
            ClearSlots();
            _current = null;
            _committed = false;
            narrativeView?.Clear();
            attributeView?.Clear();
            handTray?.ClearTypeFilter();
            HideImmediate();
        }

        public void Close() => Cancel();

        void HideImmediate()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (dimmer != null)
                dimmer.SetActive(false);
        }

        void RebuildSlots(EventData data)
        {
            ClearSlots();
            if (cardSlotPanel == null || cardSlotPrefab == null || data.slots == null)
                return;

            for (int i = 0; i < data.slots.Count; i++)
            {
                var slotData = data.slots[i];
                if (slotData == null)
                    continue;

                var slot = Instantiate(cardSlotPrefab, cardSlotPanel);
                slot.gameObject.SetActive(true);
                slot.Setup(slotData);
                slot.Clicked += OnSlotClicked;
                slot.CardPlaced += OnCardPlaced;
                slot.CardRemoved += OnCardRemoved;
                _spawnedSlots.Add(slot);
            }
        }

        void ClearSlots()
        {
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                var slot = _spawnedSlots[i];
                if (slot == null)
                    continue;
                slot.Clicked -= OnSlotClicked;
                slot.CardPlaced -= OnCardPlaced;
                slot.CardRemoved -= OnCardRemoved;
                Destroy(slot.gameObject);
            }

            _spawnedSlots.Clear();

            if (cardSlotPanel == null)
                return;

            for (int i = cardSlotPanel.childCount - 1; i >= 0; i--)
                Destroy(cardSlotPanel.GetChild(i).gameObject);
        }

        void ReturnAllSlotCardsToHand()
        {
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                var slot = _spawnedSlots[i];
                if (slot == null)
                    continue;
                var card = slot.Clear();
                card?.ReturnToHand();
            }
        }

        bool HasAllRequiredSlotsFilled()
        {
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                var slot = _spawnedSlots[i];
                if (slot != null && slot.Required && slot.Occupant == null)
                    return false;
            }

            return true;
        }

        void OnSlotClicked(EventCardSlot slot)
        {
            if (handTray == null || slot == null || _committed)
                return;
            handTray.SetTypeFilter(slot.AcceptType);
        }

        void OnCardPlaced(EventCardSlot slot, HandCardView card)
        {
            RefreshAttributesAndPreview();
        }

        void OnCardRemoved(EventCardSlot slot, HandCardView card)
        {
            RefreshAttributesAndPreview();
        }

        void RefreshAttributesAndPreview()
        {
            if (_current == null)
                return;

            var provided = SumProvidedAttributes();
            attributeView?.Show(_current.requiredAttributes, provided);

            bool anyCard = false;
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                if (_spawnedSlots[i] != null && _spawnedSlots[i].Occupant != null)
                {
                    anyCard = true;
                    break;
                }
            }

            if (anyCard && !string.IsNullOrEmpty(_current.previewOutcomeText))
                narrativeView?.SetPreviewOutcome(_current.previewOutcomeText);
            else
                narrativeView?.ClearPreview();
        }

        ElementRequirement SumProvidedAttributes()
        {
            var sum = new ElementRequirement();
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                var occ = _spawnedSlots[i]?.Occupant;
                var data = occ?.Data;
                if (data == null)
                    continue;
                sum.metal += data.elementMetal;
                sum.wood += data.elementWood;
                sum.water += data.elementWater;
                sum.fire += data.elementFire;
                sum.earth += data.elementEarth;
            }

            return sum;
        }

        public EventData CurrentEvent => _current;
        public bool IsCommitted => _committed;
    }
}
