using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using XHHY.Game.Data;
using XHHY.Game.Interaction;
using XHHY.Game.Motion;
using XHHY.Game.Runtime;

namespace XHHY.Game.UI
{
    /// <summary>
    /// 只计算手牌目标位置。位移由 HandCardView 弹簧过去。拖拽期间不重算。
    /// </summary>
    public class HandTrayController : MonoBehaviour
    {
        [SerializeField] RectTransform handCardContainer;
        [SerializeField] HandCardView cardPrefab;
        [SerializeField] DeckConfig deckConfig;
        [SerializeField] HandLayoutSettings layoutSettings;
        [SerializeField] MotionSettings motionSettings;
        [SerializeField] UiSfx uiSfx;

        readonly List<HandCardView> _cards = new List<HandCardView>();
        readonly List<HandCardView> _visible = new List<HandCardView>();
        CardTypeDefinition _activeFilter;
        string _activeGroupFilter;
        EventSlotData _acceptSlot;
        bool _built;
        bool _frozen;
        bool _dirty = true;
        int _hoverIndex = -1;
        float _originX;
        float _step = 1f;
        GameState _state;

        public int LayoutComputeCount { get; private set; }
        public IReadOnlyList<HandCardView> Cards => _cards;
        public RectTransform HandCardContainer => handCardContainer;
        public MotionSettings Motion => motionSettings;
        public UiSfx Sfx => uiSfx;
        public CardTypeDefinition ActiveFilter => _activeFilter;

        HandLayoutSettings Settings
        {
            get
            {
                if (layoutSettings == null)
                    layoutSettings = ScriptableObject.CreateInstance<HandLayoutSettings>();
                return layoutSettings;
            }
        }

        void Awake()
        {
            EnsureRaycastBlocker();
        }

        void Start()
        {
            if (!_built && deckConfig != null)
                BuildFromDeck(deckConfig);
            else
                ApplyTargets(true);
        }

        void Update()
        {
            if (handCardContainer == null)
                return;

            if (!_frozen)
            {
                CollectVisible();
                int next = ResolveHoverIndex();
                if (next != _hoverIndex || _dirty)
                {
                    _hoverIndex = next;
                    ApplyTargets(false);
                    _dirty = false;
                }
            }

            float dt = Time.unscaledDeltaTime;
            var motion = motionSettings != null ? motionSettings : MotionSettings.CreateRuntimeDefaults();
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card == null || card.IsInSlot || card.LayoutLocked)
                    continue;
                card.TickLayout(motion, dt);
            }
        }

        void EnsureRaycastBlocker()
        {
            EnsureImageRaycast(gameObject, new Color(1f, 1f, 1f, 0.01f));
            if (handCardContainer != null)
                EnsureImageRaycast(handCardContainer.gameObject, new Color(1f, 1f, 1f, 0.01f));
        }

        static void EnsureImageRaycast(GameObject go, Color color)
        {
            if (go == null)
                return;
            var image = go.GetComponent<Image>();
            if (image == null)
            {
                if (go.GetComponent<CanvasRenderer>() == null)
                    go.AddComponent<CanvasRenderer>();
                image = go.AddComponent<Image>();
                image.color = color;
                image.sprite = null;
            }

            image.raycastTarget = true;
        }

        public void NotifyDragBegan(HandCardView card)
        {
            _frozen = true;
            card?.SetLayoutLocked(true);
        }

        public void NotifyDragEnded()
        {
            _frozen = false;
            _dirty = false;
            ApplyTargets(false);
        }

        public void BuildFromDeck(DeckConfig config)
        {
            ClearAll();
            ClearContainerPlaceholders();
            EnsureRaycastBlocker();

            deckConfig = config;
            _state = GameState.Ensure();
            _state.LoadFromDeck(config);
            RebuildViewsFromState();

            _built = true;
            _dirty = true;
            ApplyTargets(true);
            ApplyFilter();
        }

        public void RebuildViewsFromState()
        {
            ClearViewsOnly();
            if (_state == null)
                _state = GameState.Ensure();

            var hand = _state.GetByLocation(CardLocation.Hand);
            var stacked = new Dictionary<string, CardInstance>();
            var singles = new List<CardInstance>();

            for (int i = 0; i < hand.Count; i++)
            {
                var inst = hand[i];
                if (inst?.data == null)
                    continue;
                if (inst.data.IsStackable)
                {
                    if (stacked.TryGetValue(inst.data.cardId, out var existing))
                    {
                        existing.count += inst.count;
                        inst.location = CardLocation.Consumed;
                    }
                    else
                    {
                        stacked[inst.data.cardId] = inst;
                    }
                }
                else
                {
                    singles.Add(inst);
                }
            }

            foreach (var kv in stacked)
                SpawnCard(kv.Value, kv.Value.count);
            for (int i = 0; i < singles.Count; i++)
                SpawnCard(singles[i], 1);
        }

        void RefreshStackViews()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var v = _cards[i];
                if (v?.Instance != null && v.Instance.data != null && v.Instance.data.IsStackable)
                    v.SetDisplayCount(v.Instance.count);
            }
        }

        public HandCardView SpawnCard(CardData data)
        {
            var view = Instantiate(cardPrefab, handCardContainer);
            view.gameObject.SetActive(true);
            view.Bind(data, this);
            view.GetComponent<HandCardDrag>()?.SetMotion(motionSettings, uiSfx);
            _cards.Add(view);
            _dirty = true;
            return view;
        }

        public HandCardView SpawnCard(CardInstance instance, int displayCount)
        {
            var view = Instantiate(cardPrefab, handCardContainer);
            view.gameObject.SetActive(true);
            view.Bind(instance, this, displayCount);
            view.GetComponent<HandCardDrag>()?.SetMotion(motionSettings, uiSfx);
            _cards.Add(view);
            _dirty = true;
            return view;
        }

        public void ReturnCard(HandCardView card)
        {
            if (card == null)
                return;

            if (card.Instance != null && _state != null)
            {
                if (card.Instance.data != null && card.Instance.data.IsStackable)
                {
                    var stack = _state.FindStackInHand(card.Instance.data.cardId);
                    HandCardView stackView = null;
                    if (stack != null && stack != card.Instance)
                    {
                        for (int i = 0; i < _cards.Count; i++)
                        {
                            var other = _cards[i];
                            if (other != null && other != card && other.Instance == stack)
                            {
                                stackView = other;
                                break;
                            }
                        }
                    }

                    if (stackView != null)
                    {
                        stack.count += Mathf.Max(1, card.Instance.count);
                        card.Instance.location = CardLocation.Consumed;
                        stackView.SetDisplayCount(stack.count);
                        Destroy(card.gameObject);
                        _cards.Remove(card);
                        _frozen = false;
                        _dirty = true;
                        ApplyTargets(false);
                        return;
                    }
                }
                _state.MoveCard(card.Instance, CardLocation.Hand);
            }

            if (!_cards.Contains(card))
                _cards.Add(card);

            var rt = (RectTransform)card.transform;
            rt.SetParent(handCardContainer, true);
            rt.anchorMin = new Vector2(0.5f, 0f);
            rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0f);
            card.gameObject.SetActive(true);
            card.SetLayoutLocked(false);
            card.CaptureSpringFromTransform();
            _frozen = false;
            _dirty = true;
            ApplyTargets(false);
        }

        public void NotifyCardLeftHand(HandCardView card)
        {
            if (card?.Instance != null && _state != null)
            {
                var inst = card.Instance;
                if (inst.data != null && inst.data.IsStackable && inst.count > 1)
                {
                    int remain = inst.count - 1;
                    inst.count = 1;
                    card.SetDisplayCount(1);
                    var rest = CardInstance.Create(inst.data, CardLocation.Hand, remain);
                    _state.GrantTakenFromStack(rest);
                    SpawnCard(rest, remain);
                }
                _state.MoveCard(inst, CardLocation.Slot);
            }
            if (_frozen)
                return;
            _dirty = true;
            ApplyTargets(false);
        }

        public void SetTypeFilter(CardTypeDefinition type)
        {
            _activeFilter = type;
            _activeGroupFilter = null;
            _acceptSlot = null;
            ApplyFilter();
            uiSfx?.PlayToggle();
        }

        public void SetAcceptFilter(EventSlotData slot)
        {
            _acceptSlot = slot;
            _activeFilter = null;
            _activeGroupFilter = null;
            ApplyFilter();
            uiSfx?.PlayToggle();
        }

        public bool IsFiltering(EventSlotData slot) => _acceptSlot != null && _acceptSlot == slot;

        public void SetGroupFilter(string groupId)
        {
            _activeGroupFilter = groupId;
            _activeFilter = null;
            _acceptSlot = null;
            ApplyFilter();
            uiSfx?.PlayToggle();
        }

        public void ClearTypeFilter()
        {
            _activeFilter = null;
            _activeGroupFilter = null;
            _acceptSlot = null;
            ApplyFilter();
        }

        public void BringToFront() => transform.SetAsLastSibling();

        public int CountByType(CardTypeDefinition type)
        {
            int n = 0;
            for (int i = 0; i < _cards.Count; i++)
            {
                var c = _cards[i];
                if (c == null || c.IsInSlot || c.Data?.cardType == null)
                    continue;
                if (type == null || c.Data.cardType.typeId == type.typeId)
                    n += Mathf.Max(1, c.DisplayCount);
            }
            return n;
        }

        void ApplyFilter()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card == null || card.IsInSlot)
                    continue;

                bool show = true;
                if (_acceptSlot != null)
                {
                    show = _acceptSlot.Accepts(card.Data);
                }
                else if (_activeFilter != null)
                {
                    show = card.Data != null && card.Data.cardType != null
                           && card.Data.cardType.typeId == _activeFilter.typeId;
                }
                else if (!string.IsNullOrEmpty(_activeGroupFilter))
                {
                    show = card.Data != null && card.Data.cardType != null
                           && card.Data.cardType.groupId == _activeGroupFilter;
                }

                card.SetVisible(show);
            }

            _dirty = true;
            if (!_frozen)
                ApplyTargets(false);
        }

        public void ApplyTargets(bool snap)
        {
            if (_frozen || handCardContainer == null)
                return;

            CollectVisible();
            int count = _visible.Count;
            LayoutComputeCount++;
            if (count == 0)
                return;

            var settings = Settings;
            float cardWidth = ((RectTransform)_visible[0].transform).rect.width;
            if (cardWidth < 1f)
                cardWidth = settings.fallbackCardWidth;

            float trayWidth = handCardContainer.rect.width;
            if (trayWidth < 1f)
                trayWidth = 1600f;

            float gap = Mathf.Max(0f, settings.gap);
            float natural = count * cardWidth + Mathf.Max(0, count - 1) * gap;
            float step = cardWidth + gap;
            if (count > 1 && natural > trayWidth)
                step = (trayWidth - cardWidth) / (count - 1);

            _step = Mathf.Max(1f, step);
            float span = Mathf.Max(0, count - 1) * _step;
            _originX = -span * 0.5f;

            var baseX = new float[count];
            var posX = new float[count];
            for (int i = 0; i < count; i++)
                baseX[i] = posX[i] = _originX + i * _step;

            if (_hoverIndex >= 0 && _hoverIndex < count)
            {
                for (int i = 0; i < count; i++)
                {
                    int dist = Mathf.Abs(i - _hoverIndex);
                    if (dist == 0)
                        continue;
                    float falloff = Falloff(dist);
                    float sign = i < _hoverIndex ? -1f : 1f;
                    posX[i] = baseX[i] + sign * settings.push * falloff;
                }

                float half = trayWidth * 0.5f;
                float minCenter = -half + cardWidth * 0.5f;
                float maxCenter = half - cardWidth * 0.5f;
                float overL = minCenter - posX[0];
                float overR = posX[count - 1] - maxCenter;
                float over = Mathf.Max(overL, overR);
                if (over > 0f)
                {
                    float peak = 0f;
                    for (int i = 0; i < count; i++)
                        peak = Mathf.Max(peak, Mathf.Abs(posX[i] - baseX[i]));
                    float scale = peak <= 0.001f ? 0f : Mathf.Clamp01((peak - over) / peak);
                    for (int i = 0; i < count; i++)
                        posX[i] = baseX[i] + (posX[i] - baseX[i]) * scale;
                }
            }

            for (int i = 0; i < count; i++)
            {
                bool hovered = i == _hoverIndex;
                float y = settings.baseY + (hovered ? settings.hoverLift : 0f);
                float scale = hovered ? settings.hoverScale : 1f;
                _visible[i].SetLayoutTarget(new Vector2(posX[i], y), scale, snap);
                _visible[i].transform.SetSiblingIndex(i);
            }

            if (_hoverIndex >= 0 && _hoverIndex < count)
                _visible[_hoverIndex].transform.SetAsLastSibling();
        }

        float Falloff(int distance)
        {
            var settings = Settings;
            if (distance == 1) return settings.falloff1;
            if (distance == 2) return settings.falloff2;
            if (distance == 3) return settings.falloff3;
            return 0f;
        }

        void CollectVisible()
        {
            _visible.Clear();
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card == null || card.IsInSlot || card.LayoutLocked || !card.gameObject.activeSelf)
                    continue;
                _visible.Add(card);
            }
        }

        int ResolveHoverIndex()
        {
            if (_visible.Count == 0 || Mouse.current == null)
                return -1;

            var canvas = handCardContainer.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera;

            Vector2 screen = Mouse.current.position.ReadValue();
            if (!RectTransformUtility.RectangleContainsScreenPoint(handCardContainer, screen, cam))
                return -1;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    handCardContainer, screen, cam, out var local))
                return -1;

            int count = _visible.Count;
            float step = Mathf.Max(1f, _step);
            int nearest = Mathf.Clamp(Mathf.RoundToInt((local.x - _originX) / step), 0, count - 1);
            if (_hoverIndex < 0 || _hoverIndex >= count)
                return nearest;

            float center = _originX + _hoverIndex * step;
            float delta = local.x - center;
            float gate = step * Settings.hysteresis;
            if (Mathf.Abs(delta) < gate)
                return _hoverIndex;

            int dir = delta > 0f ? 1 : -1;
            return Mathf.Clamp(_hoverIndex + dir, 0, count - 1);
        }

        void ClearContainerPlaceholders()
        {
            if (handCardContainer == null)
                return;

            for (int i = handCardContainer.childCount - 1; i >= 0; i--)
                Destroy(handCardContainer.GetChild(i).gameObject);
        }

        void ClearViewsOnly()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null)
                    Destroy(_cards[i].gameObject);
            }
            _cards.Clear();
            _visible.Clear();
            _hoverIndex = -1;
        }

        void ClearAll()
        {
            ClearViewsOnly();
            _built = false;
            _dirty = true;
        }
    }
}
