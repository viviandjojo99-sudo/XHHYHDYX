using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XHHY.Game.Data;

namespace XHHY.Game.UI
{
    /// <summary>
    /// 挂在 HandCardType 上：点击后按类型过滤手牌。type 为空表示「全部」。
    /// </summary>
    public class HandCardTagItem : MonoBehaviour, IPointerClickHandler
    {
        [SerializeField] CardTypeDefinition type;
        [SerializeField] Image iconImage;
        [SerializeField] TMP_FontAsset labelFont;
        [SerializeField] TMP_Text labelText;
        [SerializeField] TMP_Text countText;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] HandTrayController handTray;

        public CardTypeDefinition Type => type;

        public void Setup(CardTypeDefinition cardType, HandTrayController tray, int count = -1, bool dimmed = false)
        {
            type = cardType;
            handTray = tray;
            EnsureLabel();
            HideIcon();

            if (labelText != null)
            {
                ApplyFont(labelText);
                labelText.text = type == null ? "全部" : type.displayName;
                labelText.gameObject.SetActive(true);
            }

            if (countText != null)
            {
                if (count >= 0)
                {
                    countText.gameObject.SetActive(true);
                    countText.text = count.ToString();
                }
                else
                {
                    countText.gameObject.SetActive(false);
                }
            }

            if (canvasGroup == null)
                canvasGroup = GetComponent<CanvasGroup>();
            if (canvasGroup != null)
                canvasGroup.alpha = dimmed ? 0.35f : 1f;
        }

        void EnsureLabel()
        {
            if (labelText != null)
                return;

            var existing = transform.Find("TagLabel");
            if (existing != null)
                labelText = existing.GetComponent<TMP_Text>();
            if (labelText != null)
                return;

            var go = new GameObject("TagLabel", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, -2f);

            labelText = go.AddComponent<TextMeshProUGUI>();
            labelText.alignment = TextAlignmentOptions.Center;
            labelText.fontSize = 22;
            labelText.color = new Color(0.12f, 0.1f, 0.08f, 1f);
            labelText.raycastTarget = false;
            labelText.enableWordWrapping = false;
            labelText.overflowMode = TextOverflowModes.Ellipsis;
            ApplyFont(labelText);
            go.transform.SetAsLastSibling();
        }

        void HideIcon()
        {
            if (iconImage == null)
                iconImage = transform.Find("CardTypeIcon")?.GetComponent<Image>();
            if (iconImage == null)
                return;

            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        static TMP_FontAsset _projectFont;

        void ApplyFont(TMP_Text text)
        {
            var font = labelFont != null ? labelFont : ProjectFont();
            if (font == null || text == null)
                return;
            text.font = font;
        }

        static TMP_FontAsset ProjectFont()
        {
            if (_projectFont != null)
                return _projectFont;

            var texts = FindObjectsByType<TMP_Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < texts.Length; i++)
            {
                var font = texts[i] != null ? texts[i].font : null;
                if (font != null && font.name.Contains("ChillHuoFangSong_ConRegular"))
                {
                    _projectFont = font;
                    return font;
                }
            }

            return TMP_Settings.defaultFontAsset;
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
