using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XHHY.Game.Data;

namespace XHHY.Game.UI
{
    public class EventCardSlot : MonoBehaviour, IDropHandler, IPointerClickHandler
    {
        [SerializeField] Image typeIcon;
        [SerializeField] Transform cardAnchor;

        public string SlotId { get; private set; }
        public EventSlotData SlotData { get; private set; }
        public CardTypeDefinition AcceptType { get; private set; }
        public bool Required { get; private set; }
        public HandCardView Occupant { get; private set; }
        public bool CreatedAtRuntime { get; private set; }

        public System.Action<EventCardSlot> Clicked;
        public System.Action<EventCardSlot, HandCardView> CardPlaced;
        public System.Action<EventCardSlot, HandCardView> CardRemoved;

        void Awake()
        {
            var graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                if (graphics[i] != null && graphics[i].gameObject != gameObject)
                    graphics[i].raycastTarget = false;
            }
        }

        public void MarkCreatedAtRuntime() => CreatedAtRuntime = true;

        public void Setup(EventSlotData data) => Setup(data, true);

        public void Setup(EventSlotData data, bool moveToDataPosition)
        {
            SlotData = data;
            SlotId = data != null ? data.slotId : null;
            AcceptType = data != null ? data.PrimaryAcceptType : null;
            Required = data != null && data.required;

            if (moveToDataPosition && data != null)
            {
                var rt = (RectTransform)transform;
                rt.anchoredPosition = data.anchoredPosition;
            }

            var rootImage = GetComponent<Image>();
            if (rootImage != null)
                rootImage.raycastTarget = true;

            if (typeIcon != null && AcceptType != null && AcceptType.icon != null)
                typeIcon.sprite = AcceptType.icon;
            RefreshTypeIcon();
        }

        void RefreshTypeIcon()
        {
            if (typeIcon == null)
                return;
            typeIcon.enabled = Occupant == null && typeIcon.sprite != null;
        }

        public bool ContainsScreen(Vector2 screen)
        {
            return ScreenRectOf((RectTransform)transform).Contains(screen);
        }

        public static EventCardSlot FindUnderPointer(Vector2 screen, CardData data)
        {
            var hovered = RaycastSlot(screen, data);
            if (hovered != null)
                return hovered;

            var slots = FindObjectsByType<EventCardSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot != null && slot.CanAccept(data) && slot.ContainsScreen(screen))
                    return slot;
            }

            return null;
        }

        public static EventCardSlot FindForCard(RectTransform card, Vector2 pointerScreen, CardData data)
        {
            if (card == null)
                return null;

            var hovered = RaycastSlot(pointerScreen, data);
            if (hovered != null)
                return hovered;

            var slots = FindObjectsByType<EventCardSlot>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            var cardRect = ScreenRectOf(card);
            EventCardSlot best = null;
            float bestScore = 0f;

            for (int i = 0; i < slots.Length; i++)
            {
                var slot = slots[i];
                if (slot == null || !slot.CanAccept(data))
                    continue;

                var slotRect = ScreenRectOf((RectTransform)slot.transform);
                slotRect.xMin -= 48f;
                slotRect.yMin -= 48f;
                slotRect.xMax += 48f;
                slotRect.yMax += 48f;

                float score = OverlapArea(cardRect, slotRect);
                if (slotRect.Contains(pointerScreen))
                    score += 100000f;
                if (score > bestScore)
                {
                    bestScore = score;
                    best = slot;
                }
            }

            return best;
        }

        static EventCardSlot RaycastSlot(Vector2 screen, CardData data)
        {
            if (EventSystem.current == null)
                return null;

            var pointer = new PointerEventData(EventSystem.current) { position = screen };
            var hits = new List<RaycastResult>();
            EventSystem.current.RaycastAll(pointer, hits);
            for (int i = 0; i < hits.Count; i++)
            {
                var go = hits[i].gameObject;
                if (go == null)
                    continue;
                var slot = go.GetComponent<EventCardSlot>() ?? go.GetComponentInParent<EventCardSlot>();
                if (slot != null && slot.CanAccept(data))
                    return slot;
            }

            return null;
        }

        static Rect ScreenRectOf(RectTransform rt)
        {
            var canvas = rt.GetComponentInParent<Canvas>();
            Camera cam = null;
            if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = canvas.worldCamera != null ? canvas.worldCamera : Camera.main;

            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            var bl = RectTransformUtility.WorldToScreenPoint(cam, corners[0]);
            var tr = RectTransformUtility.WorldToScreenPoint(cam, corners[2]);
            return Rect.MinMaxRect(
                Mathf.Min(bl.x, tr.x),
                Mathf.Min(bl.y, tr.y),
                Mathf.Max(bl.x, tr.x),
                Mathf.Max(bl.y, tr.y));
        }

        static float OverlapArea(Rect a, Rect b)
        {
            float x = Mathf.Min(a.xMax, b.xMax) - Mathf.Max(a.xMin, b.xMin);
            float y = Mathf.Min(a.yMax, b.yMax) - Mathf.Max(a.yMin, b.yMin);
            if (x <= 0f || y <= 0f)
                return 0f;
            return x * y;
        }

        public bool CanAccept(CardData card)
        {
            if (card == null || Occupant != null)
                return false;
            if (SlotData != null)
                return SlotData.Accepts(card);
            if (AcceptType == null || card.cardType == null)
                return false;
            return card.cardType.typeId == AcceptType.typeId;
        }

        public void Place(HandCardView cardView)
        {
            if (cardView == null)
                return;

            Occupant = cardView;
            var anchor = cardAnchor != null ? cardAnchor : transform;
            cardView.AttachToSlot(anchor);
            RefreshTypeIcon();
            CardPlaced?.Invoke(this, cardView);
        }

        public HandCardView Clear()
        {
            var previous = Occupant;
            Occupant = null;
            RefreshTypeIcon();
            if (previous != null)
                CardRemoved?.Invoke(this, previous);
            return previous;
        }

        public void OnDrop(PointerEventData eventData)
        {
            var card = eventData.pointerDrag != null
                ? eventData.pointerDrag.GetComponent<HandCardView>()
                : null;
            if (card == null)
                return;

            if (CanAccept(card.Data))
                Place(card);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(this);
        }
    }
}
