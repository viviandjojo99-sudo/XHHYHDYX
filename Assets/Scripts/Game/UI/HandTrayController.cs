using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using XHHY.Game.Data;
using XHHY.Game.Interaction;

namespace XHHY.Game.UI
{
    /// <summary>
    /// 手牌栏：平铺；超出宽度则挤压堆叠；鼠标滑过附近卡牌局部展开（苏丹式）。
    /// 不改鼠标坐标，只改卡牌 anchoredPosition。
    /// </summary>
    public class HandTrayController : MonoBehaviour
    {
        [SerializeField] RectTransform handCardContainer;
        [SerializeField] HandCardView cardPrefab;
        [SerializeField] HandDeckConfig deckConfig;

        [Header("布局")]
        [SerializeField] float preferredSpacing = 148f;
        [SerializeField] float minSpacing = 36f;
        [SerializeField] float sidePadding = 20f;
        [SerializeField] float baseY = 10f;
        [SerializeField] float hoverExpand = 90f;
        [SerializeField] float hoverLift = 36f;
        [SerializeField] float hoverFalloff = 1.25f;

        readonly List<HandCardView> _cards = new List<HandCardView>();
        CardTypeDefinition _activeFilter;
        bool _built;
        int _hoverIndex = -1;

        public IReadOnlyList<HandCardView> Cards => _cards;
        public RectTransform HandCardContainer => handCardContainer;

        void Awake()
        {
            EnsureRaycastBlocker();
        }

        void Start()
        {
            if (!_built && deckConfig != null)
                BuildFromDeck(deckConfig);
            else
                Relayout();
        }

        void Update()
        {
            if (handCardContainer == null || _cards.Count == 0)
                return;
            if (HandCardDrag.AnyDragging)
                return;

            int next = ResolveHoverIndex();
            if (next == _hoverIndex)
                return;
            _hoverIndex = next;
            Relayout();
        }

        /// <summary>HandTray / HandCard 必须有 Graphic 挡射线，否则点手牌区会拖到地图。</summary>
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

        public void BuildFromDeck(HandDeckConfig config)
        {
            ClearAll();
            ClearContainerPlaceholders();
            EnsureRaycastBlocker();

            if (config == null || config.startingCards == null || cardPrefab == null || handCardContainer == null)
                return;

            for (int i = 0; i < config.startingCards.Count; i++)
            {
                var data = config.startingCards[i];
                if (data == null)
                    continue;
                SpawnCard(data);
            }

            _built = true;
            Relayout();
            ApplyFilter();
        }

        public HandCardView SpawnCard(CardData data)
        {
            var view = Instantiate(cardPrefab, handCardContainer);
            view.gameObject.SetActive(true);
            view.Bind(data, this);
            _cards.Add(view);
            return view;
        }

        public void ReturnCard(HandCardView card)
        {
            if (card == null)
                return;

            if (!_cards.Contains(card))
                _cards.Add(card);

            card.transform.SetParent(handCardContainer, false);
            card.gameObject.SetActive(true);
            Relayout();
            ApplyFilter();
        }

        public void NotifyCardLeftHand(HandCardView card)
        {
            Relayout();
            ApplyFilter();
        }

        public void SetTypeFilter(CardTypeDefinition type)
        {
            _activeFilter = type;
            ApplyFilter();
        }

        public void ClearTypeFilter()
        {
            _activeFilter = null;
            ApplyFilter();
        }

        public void BringToFront()
        {
            transform.SetAsLastSibling();
        }

        void ApplyFilter()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card == null || card.IsInSlot)
                    continue;

                bool show = _activeFilter == null
                            || (card.Data != null
                                && card.Data.cardType != null
                                && card.Data.cardType.typeId == _activeFilter.typeId);
                card.SetVisible(show);
            }

            Relayout();
        }

        public void Relayout()
        {
            if (handCardContainer == null)
                return;

            var visible = CollectVisible();
            int count = visible.Count;
            if (count == 0)
                return;

            float cardWidth = ((RectTransform)visible[0].transform).rect.width;
            if (cardWidth < 1f)
                cardWidth = 140f;

            float avail = Mathf.Max(0f, handCardContainer.rect.width - sidePadding * 2f);
            float spacing = preferredSpacing;
            if (count > 1)
            {
                float fit = (avail - cardWidth) / (count - 1);
                spacing = Mathf.Clamp(fit, minSpacing, preferredSpacing);
            }

            float totalWidth = cardWidth + Mathf.Max(0, count - 1) * spacing;
            float startX = -totalWidth * 0.5f + cardWidth * 0.5f;

            for (int i = 0; i < count; i++)
            {
                var rt = (RectTransform)visible[i].transform;
                rt.anchorMin = new Vector2(0.5f, 0f);
                rt.anchorMax = new Vector2(0.5f, 0f);
                rt.pivot = new Vector2(0.5f, 0f);
                rt.localRotation = Quaternion.identity;
                rt.localScale = Vector3.one;

                float x = startX + i * spacing;
                float y = baseY;

                if (_hoverIndex >= 0 && count > 1)
                {
                    float dist = Mathf.Abs(i - _hoverIndex);
                    float w = Mathf.Exp(-dist * hoverFalloff);
                    // 左右推开，悬停卡抬高
                    if (i < _hoverIndex)
                        x -= hoverExpand * w;
                    else if (i > _hoverIndex)
                        x += hoverExpand * w;
                    else
                        y += hoverLift;
                }

                rt.anchoredPosition = new Vector2(x, y);
                rt.SetSiblingIndex(i);
            }

            // 悬停卡画在最前
            if (_hoverIndex >= 0 && _hoverIndex < count)
                visible[_hoverIndex].transform.SetAsLastSibling();
        }

        List<HandCardView> CollectVisible()
        {
            var visible = new List<HandCardView>();
            for (int i = 0; i < _cards.Count; i++)
            {
                var card = _cards[i];
                if (card == null || card.IsInSlot || !card.gameObject.activeSelf)
                    continue;
                visible.Add(card);
            }

            return visible;
        }

        int ResolveHoverIndex()
        {
            var visible = CollectVisible();
            if (visible.Count == 0 || Mouse.current == null)
                return -1;

            if (!RectTransformUtility.RectangleContainsScreenPoint(
                    handCardContainer,
                    Mouse.current.position.ReadValue(),
                    null))
                return -1;

            float bestDist = float.MaxValue;
            int best = -1;
            for (int i = 0; i < visible.Count; i++)
            {
                var rt = (RectTransform)visible[i].transform;
                Vector2 screen = RectTransformUtility.WorldToScreenPoint(null, rt.position);
                float dx = Mathf.Abs(Mouse.current.position.ReadValue().x - screen.x);
                if (dx < bestDist)
                {
                    bestDist = dx;
                    best = i;
                }
            }

            return best;
        }

        void ClearContainerPlaceholders()
        {
            if (handCardContainer == null)
                return;

            for (int i = handCardContainer.childCount - 1; i >= 0; i--)
                Destroy(handCardContainer.GetChild(i).gameObject);
        }

        void ClearAll()
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null)
                    Destroy(_cards[i].gameObject);
            }

            _cards.Clear();
            _built = false;
            _hoverIndex = -1;
        }
    }
}
