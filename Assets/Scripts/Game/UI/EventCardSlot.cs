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
        public CardTypeDefinition AcceptType { get; private set; }
        public bool Required { get; private set; }
        public HandCardView Occupant { get; private set; }

        public System.Action<EventCardSlot> Clicked;
        public System.Action<EventCardSlot, HandCardView> CardPlaced;
        public System.Action<EventCardSlot, HandCardView> CardRemoved;

        public void Setup(EventSlotData data)
        {
            SlotId = data.slotId;
            AcceptType = data.acceptType;
            Required = data.required;

            var rt = (RectTransform)transform;
            rt.anchoredPosition = data.anchoredPosition;

            if (typeIcon != null)
                typeIcon.sprite = data.acceptType != null ? data.acceptType.icon : null;
        }

        public bool CanAccept(CardData card)
        {
            if (card == null || AcceptType == null || card.cardType == null)
                return false;
            if (Occupant != null)
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
            CardPlaced?.Invoke(this, cardView);
        }

        public HandCardView Clear()
        {
            var previous = Occupant;
            Occupant = null;
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

            if (!CanAccept(card.Data))
            {
                card.ReturnToHand();
                return;
            }

            Place(card);
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            Clicked?.Invoke(this);
        }
    }
}
