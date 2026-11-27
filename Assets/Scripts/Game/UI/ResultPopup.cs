using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace XHHY.Game.UI
{
    public class ResultPopup : MonoBehaviour
    {
        static ResultPopup _instance;

        [SerializeField] GameObject root;
        [SerializeField] TMP_Text bodyText;
        [SerializeField] Button closeButton;

        void Awake()
        {
            _instance = this;
            if (root == null)
                root = gameObject;
            if (closeButton != null)
            {
                closeButton.onClick.RemoveListener(Hide);
                closeButton.onClick.AddListener(Hide);
            }
            Hide();
        }

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public static void Show(string text)
        {
            if (_instance == null)
            {
                EnsureRuntime();
            }
            _instance.ShowInternal(text);
        }

        void ShowInternal(string text)
        {
            if (bodyText != null)
                bodyText.text = text ?? string.Empty;
            if (root != null)
                root.SetActive(true);
            gameObject.SetActive(true);
        }

        public void Hide()
        {
            if (root != null)
                root.SetActive(false);
        }

        static void EnsureRuntime()
        {
            var canvas = Object.FindFirstObjectByType<Canvas>();
            var go = new GameObject("ResultPopup", typeof(RectTransform), typeof(ResultPopup));
            if (canvas != null)
                go.transform.SetParent(canvas.transform, false);
            var popup = go.GetComponent<ResultPopup>();
            var rt = (RectTransform)go.transform;
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(520, 280);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.12f, 0.12f, 0.14f, 0.94f);

            var textGo = new GameObject("Body", typeof(RectTransform));
            textGo.transform.SetParent(go.transform, false);
            var textRt = (RectTransform)textGo.transform;
            textRt.anchorMin = new Vector2(0.08f, 0.28f);
            textRt.anchorMax = new Vector2(0.92f, 0.9f);
            textRt.offsetMin = Vector2.zero;
            textRt.offsetMax = Vector2.zero;
            var tmp = textGo.AddComponent<TextMeshProUGUI>();
            tmp.fontSize = 28;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;

            var btnGo = new GameObject("Close", typeof(RectTransform));
            btnGo.transform.SetParent(go.transform, false);
            var btnRt = (RectTransform)btnGo.transform;
            btnRt.anchorMin = new Vector2(0.35f, 0.06f);
            btnRt.anchorMax = new Vector2(0.65f, 0.22f);
            btnRt.offsetMin = Vector2.zero;
            btnRt.offsetMax = Vector2.zero;
            var btnImg = btnGo.AddComponent<Image>();
            btnImg.color = new Color(0.85f, 0.75f, 0.45f, 1f);
            var btn = btnGo.AddComponent<Button>();
            var btnLabelGo = new GameObject("Label", typeof(RectTransform));
            btnLabelGo.transform.SetParent(btnGo.transform, false);
            var btnLabelRt = (RectTransform)btnLabelGo.transform;
            btnLabelRt.anchorMin = Vector2.zero;
            btnLabelRt.anchorMax = Vector2.one;
            btnLabelRt.offsetMin = Vector2.zero;
            btnLabelRt.offsetMax = Vector2.zero;
            var btnLabel = btnLabelGo.AddComponent<TextMeshProUGUI>();
            btnLabel.text = "确定";
            btnLabel.alignment = TextAlignmentOptions.Center;
            btnLabel.fontSize = 24;
            btnLabel.color = Color.black;

            popup.root = go;
            popup.bodyText = tmp;
            popup.closeButton = btn;
            btn.onClick.AddListener(popup.Hide);
            _instance = popup;
        }
    }
}
