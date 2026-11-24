using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XHHY.Game.Data;
using XHHY.Game.Interaction;

namespace XHHY.Game.UI
{
    public class HandCardView : MonoBehaviour
    {
        [SerializeField] TMP_Text nameText;
        [SerializeField] Image iconImage;

        public CardData Data { get; private set; }
        public HandTrayController HomeTray { get; private set; }
        public bool IsInSlot { get; private set; }

        HandCardDrag _drag;

        public void Bind(CardData data, HandTrayController tray)
        {
            Data = data;
            HomeTray = tray;
            IsInSlot = false;
            RefreshVisual();

            _drag = GetComponent<HandCardDrag>();
            if (_drag == null)
                _drag = gameObject.AddComponent<HandCardDrag>();
            _drag.Bind(this);
        }

        public void RefreshVisual()
        {
            if (nameText != null)
                nameText.text = Data != null ? Data.title : string.Empty;
            if (iconImage != null)
                iconImage.sprite = Data != null ? Data.icon : null;
        }

        public void AttachToSlot(Transform slotAnchor)
        {
            IsInSlot = true;
            transform.SetParent(slotAnchor, false);
            var rt = (RectTransform)transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.localScale = Vector3.one;
            HomeTray?.NotifyCardLeftHand(this);
        }

        public void ReturnToHand()
        {
            IsInSlot = false;
            HomeTray?.ReturnCard(this);
        }

        public void SetVisible(bool visible)
        {
            gameObject.SetActive(visible);
        }
    }
}
