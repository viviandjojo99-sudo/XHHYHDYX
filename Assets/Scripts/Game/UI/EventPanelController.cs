using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XHHY.Game.Data;
using XHHY.Game.Motion;
using XHHY.Game.Runtime;
using XHHY.Game.Settlement;

namespace XHHY.Game.UI
{
    public class EventPanelController : MonoBehaviour
    {
        public static EventPanelController Instance { get; private set; }

        [Header("根与遮罩")]
        [SerializeField] GameObject panelRoot;
        [SerializeField] GameObject dimmer;
        [SerializeField] CanvasGroup panelCanvasGroup;

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
        [SerializeField] MotionSettings motionSettings;
        [SerializeField] UiSfx uiSfx;

        readonly List<EventCardSlot> _spawnedSlots = new List<EventCardSlot>();
        EventData _current;
        bool _committed;
        bool _animating;
        Spring1D _scaleSpring;
        Spring1D _alphaSpring;
        float _targetScale = 1f;
        float _targetAlpha = 1f;
        RectTransform _panelRt;

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
            _panelRt = panelRoot.transform as RectTransform;
            if (panelCanvasGroup == null)
                panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();

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

        void Update()
        {
            if (!_animating || _panelRt == null)
                return;
            var motion = motionSettings != null ? motionSettings : MotionSettings.CreateRuntimeDefaults();
            float dt = Time.unscaledDeltaTime;
            _scaleSpring.Step(_targetScale, motion.panelOpenStiffness, motion.panelOpenDamping, dt, motion.substeps);
            _alphaSpring.Step(_targetAlpha, motion.panelOpenStiffness, motion.panelOpenDamping, dt, motion.substeps);
            float s = Mathf.Clamp(_scaleSpring.value, 0.9f, 1.08f);
            if (float.IsNaN(s) || float.IsInfinity(s))
                s = 1f;
            _panelRt.localScale = new Vector3(s, s, 1f);
            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = Mathf.Clamp01(_alphaSpring.value);
            if (_targetAlpha <= 0.01f && _alphaSpring.NearlySettled(0f, 0.02f, 0.5f))
            {
                _animating = false;
                if (panelRoot != null)
                    panelRoot.SetActive(false);
            }
            else if (_targetAlpha >= 0.99f && _scaleSpring.NearlySettled(1f))
            {
                _animating = false;
            }
        }

        public void Open(EventData data)
        {
            if (data == null)
                return;

            if (_committed && _current == data && _spawnedSlots.Count > 0)
            {
                ShowShellAnimated();
                PresentHand();
                RefreshConfirmButton();
                return;
            }

            _current = data;
            _committed = false;
            ShowShellAnimated();
            HideCoveringEventFrames();

            if (eventBg != null)
                eventBg.sprite = data.background;

            if (eventNameText != null)
                eventNameText.text = data.title ?? string.Empty;

            narrativeView?.SetOpening(data.openingText);
            RebuildSlots(data);
            RefreshAttributesAndPreview();

            PresentHand();
            RefreshConfirmButton();
            uiSfx?.PlayToggle();
        }

        void ShowShellAnimated()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);
            if (dimmer != null)
                dimmer.SetActive(true);

            EnsureCanvasGroup();
            var motion = motionSettings != null ? motionSettings : MotionSettings.CreateRuntimeDefaults();
            _targetScale = 1f;
            _targetAlpha = 1f;
            _scaleSpring.Snap(motion.panelFromScale);
            _alphaSpring.Snap(1f);
            if (_panelRt != null)
                _panelRt.localScale = new Vector3(motion.panelFromScale, motion.panelFromScale, 1f);
            if (panelCanvasGroup != null)
                panelCanvasGroup.alpha = 1f;
            _animating = true;
        }

        void EnsureCanvasGroup()
        {
            if (panelRoot == null)
                panelRoot = gameObject;
            if (panelCanvasGroup == null)
                panelCanvasGroup = panelRoot.GetComponent<CanvasGroup>();
            if (panelCanvasGroup == null)
                panelCanvasGroup = panelRoot.AddComponent<CanvasGroup>();
            _panelRt = panelRoot.transform as RectTransform;
        }

        void PresentHand()
        {
            if (handTray == null)
                return;
            handTray.gameObject.SetActive(true);
            handTray.BringToFront();
            handTray.ClearTypeFilter();
            handTray.ApplyTargets(false);
        }

        void RefreshConfirmButton()
        {
            if (confirmButton == null)
                return;
            confirmButton.interactable = !_committed && HasAllRequiredSlotsFilled();
        }

        public void Confirm()
        {
            if (_current == null || _committed)
                return;

            if (!HasAllRequiredSlotsFilled())
            {
                Debug.Log("还有必填槽位未放入卡牌。");
                return;
            }

            var ctx = BuildContext();
            var outcome = OutcomeResolver.Resolve(_current, ctx);
            if (outcome != null)
            {
                outcome.Run(ctx);
                if (!string.IsNullOrEmpty(outcome.resultText))
                    ResultPopup.Show(outcome.resultText);
            }
            else if (!string.IsNullOrEmpty(_current.previewOutcomeText))
            {
                ResultPopup.Show(_current.previewOutcomeText);
            }

            GameState.Ensure().MarkEventCompleted(_current.eventId);
            _committed = true;
            handTray?.ClearTypeFilter();
            AnimateHide();
        }

        public void Cancel()
        {
            if (_committed)
            {
                AnimateHide();
                return;
            }

            ReturnAllSlotCardsToHand();
            ClearSlots();
            _current = null;
            _committed = false;
            narrativeView?.Clear();
            attributeView?.Clear();
            handTray?.ClearTypeFilter();
            AnimateHide();
        }

        public void Close() => Cancel();

        void AnimateHide()
        {
            if (dimmer != null)
                dimmer.SetActive(false);
            _targetScale = motionSettings != null ? motionSettings.panelFromScale : 0.96f;
            _targetAlpha = 0f;
            _animating = true;
            handTray?.ClearTypeFilter();
        }

        void HideImmediate()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
            if (dimmer != null)
                dimmer.SetActive(false);
            _animating = false;
        }

        void HideCoveringEventFrames()
        {
            if (panelRoot == null || cardSlotPanel == null)
                return;

            var root = panelRoot.transform;
            for (int i = 0; i < root.childCount; i++)
            {
                var child = root.GetChild(i);
                if (child == cardSlotPanel || cardSlotPanel.IsChildOf(child))
                {
                    child.gameObject.SetActive(true);
                    continue;
                }

                if (child.GetComponentInChildren<EventCardSlot>(true) != null)
                    child.gameObject.SetActive(false);
            }
        }

        void RebuildSlots(EventData data)
        {
            ClearSlots();
            if (cardSlotPanel == null || data == null || data.slots == null)
                return;

            var existing = new List<EventCardSlot>();
            for (int i = 0; i < cardSlotPanel.childCount; i++)
            {
                var slot = cardSlotPanel.GetChild(i).GetComponent<EventCardSlot>();
                if (slot != null)
                    existing.Add(slot);
            }

            existing.Sort((a, b) =>
            {
                float ax = ((RectTransform)a.transform).anchoredPosition.x;
                float bx = ((RectTransform)b.transform).anchoredPosition.x;
                return ax.CompareTo(bx);
            });

            int used = 0;
            for (int i = 0; i < data.slots.Count; i++)
            {
                var slotData = data.slots[i];
                if (slotData == null)
                    continue;

                EventCardSlot slot;
                bool move = false;
                if (used < existing.Count)
                {
                    slot = existing[used];
                }
                else if (cardSlotPrefab != null)
                {
                    slot = Instantiate(cardSlotPrefab, cardSlotPanel);
                    slot.MarkCreatedAtRuntime();
                    move = true;
                }
                else
                {
                    continue;
                }

                used++;
                slot.gameObject.SetActive(true);
                slot.Setup(slotData, move);
                slot.Clicked += OnSlotClicked;
                slot.CardPlaced += OnCardPlaced;
                slot.CardRemoved += OnCardRemoved;
                _spawnedSlots.Add(slot);
            }

            for (int i = used; i < existing.Count; i++)
            {
                if (existing[i] != null)
                    existing[i].gameObject.SetActive(false);
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
                var card = slot.Clear();
                card?.ReturnToHand();
                if (slot.CreatedAtRuntime)
                    Destroy(slot.gameObject);
            }

            _spawnedSlots.Clear();
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

            if (slot.Occupant != null)
                return;

            if (handTray.IsFiltering(slot.SlotData))
                handTray.ClearTypeFilter();
            else
                handTray.SetAcceptFilter(slot.SlotData);
        }

        void OnCardPlaced(EventCardSlot slot, HandCardView card) => RefreshAttributesAndPreview();
        void OnCardRemoved(EventCardSlot slot, HandCardView card) => RefreshAttributesAndPreview();

        void RefreshAttributesAndPreview()
        {
            if (_current == null)
                return;

            var ctx = BuildContext();
            attributeView?.Show(_current.ResolveRequirements(), ctx.ProvidedAttributes);

            bool anyCard = false;
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                if (_spawnedSlots[i] != null && _spawnedSlots[i].Occupant != null)
                {
                    anyCard = true;
                    break;
                }
            }

            string preview = null;
            if (anyCard)
            {
                var outcome = OutcomeResolver.Resolve(_current, ctx);
                if (outcome != null)
                {
                    switch (outcome.previewMode)
                    {
                        case PreviewMode.Show:
                            preview = outcome.previewText;
                            break;
                        case PreviewMode.Vague:
                            preview = "似乎会顺利……";
                            break;
                        case PreviewMode.Hidden:
                            preview = null;
                            break;
                    }
                }
                if (string.IsNullOrEmpty(preview))
                    preview = _current.previewOutcomeText;
            }

            if (!string.IsNullOrEmpty(preview))
                narrativeView?.SetPreviewOutcome(preview);
            else
                narrativeView?.ClearPreview();

            RefreshConfirmButton();
        }

        SettlementContext BuildContext()
        {
            var ctx = new SettlementContext
            {
                State = GameState.Ensure(),
                Event = _current
            };

            var list = new List<CardInstance>();
            for (int i = 0; i < _spawnedSlots.Count; i++)
            {
                var slot = _spawnedSlots[i];
                if (slot?.Occupant?.Instance == null && slot?.Occupant?.Data != null)
                {
                    // 无 Instance 时临时包装
                    var temp = CardInstance.Create(slot.Occupant.Data, CardLocation.Slot, 1);
                    ctx.SlotOccupants[slot.SlotId ?? i.ToString()] = temp;
                    list.Add(temp);
                }
                else if (slot?.Occupant?.Instance != null)
                {
                    ctx.SlotOccupants[slot.SlotId ?? i.ToString()] = slot.Occupant.Instance;
                    list.Add(slot.Occupant.Instance);
                }
            }

            ctx.ProvidedAttributes = GameState.Ensure().SumEffectiveAttributes(list);
            ctx.Tier = GameState.EvaluateTier(_current.ResolveRequirements(), ctx.ProvidedAttributes);
            return ctx;
        }

        public EventData CurrentEvent => _current;
        public bool IsCommitted => _committed;
    }
}
