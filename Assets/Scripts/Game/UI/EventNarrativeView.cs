using TMPro;
using UnityEngine;

namespace XHHY.Game.UI
{
    /// <summary>
    /// 打开时显示描述；放入卡牌后可追加/切换为可能结果预览。
    /// </summary>
    public class EventNarrativeView : MonoBehaviour
    {
        [SerializeField] TMP_Text contentText;

        string _opening;
        string _preview;

        public void BindText(TMP_Text text)
        {
            contentText = text;
        }

        public void SetOpening(string openingText)
        {
            _opening = openingText ?? string.Empty;
            _preview = null;
            Refresh();
        }

        public void SetPreviewOutcome(string previewText)
        {
            _preview = previewText;
            Refresh();
        }

        public void ClearPreview()
        {
            _preview = null;
            Refresh();
        }

        public void Clear()
        {
            _opening = string.Empty;
            _preview = null;
            Refresh();
        }

        void Refresh()
        {
            if (contentText == null)
                return;

            if (!string.IsNullOrEmpty(_preview))
                contentText.text = $"{_opening}\n\n—— 可能结果 ——\n{_preview}";
            else
                contentText.text = _opening;
        }
    }
}
