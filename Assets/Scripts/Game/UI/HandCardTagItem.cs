using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XHHY.Game.Data;

namespace XHHY.Game.UI
{
    /// <summary>
    /// 挂在 HandCardType 上：点击后按类型过滤手牌。
    /// type 为空表示「全部」。
    /// </summary>
    public class HandCardTagItem : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] CardTypeDefinition type;
        [SerializeField] Image iconImage;
        [SerializeField] HandTrayController handTray;

        public CardTypeDefinition Type => type;

        public void Setup(CardTypeDefinition cardType, HandTrayController tray)
        {
            type = cardType;
            handTray = tray;
            if (iconImage == null)
                iconImage = GetComponentInChildren<Image>();
            if (iconImage != null && type != null)
                iconImage.sprite = type.icon;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (handTray == null)
                handTray = FindFirstObjectByType<HandTrayController>();

            if (handTray == null)
                return;

            if (type == null)
                handTray.ClearTypeFilter();
            else
                handTray.SetTypeFilter(type);
        }
    }
}
