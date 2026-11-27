using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using XHHY.Game.Data;
using XHHY.Game.Motion;
using XHHY.Game.Runtime;

namespace XHHY.Game.UI
{
    /// <summary>
    /// 事件属性需求 vs 已填入卡牌有效属性：每行当前/需求/差值，顶部显示分档。
    /// </summary>
    public class EventAttributeView : MonoBehaviour
    {
        [SerializeField] TMP_Text summaryText;
        [SerializeField] TMP_Text tierText;
        [SerializeField] RectTransform rowContainer;
        [SerializeField] GameObject rowPrefab;
        [SerializeField] AttributeCatalog catalog;
        [SerializeField] MotionSettings motionSettings;

        readonly List<Widget> _widgets = new List<Widget>();
        GameObject _template;
        bool _cached;
        RequirementTier _tier;

        class Widget
        {
            public GameObject go;
            public Image icon;
            public TMP_Text number;
        }

        public RequirementTier CurrentTier => _tier;

        public void BindText(TMP_Text text) => summaryText = text;

        public void Show(List<AttributeRequirement> requirements, Dictionary<string, int> provided)
        {
            requirements ??= new List<AttributeRequirement>();
            provided ??= new Dictionary<string, int>();
            _tier = GameState.EvaluateTier(requirements, provided);
            CacheWidgets();

            int shown = 0;
            for (int i = 0; i < requirements.Count; i++)
            {
                var req = requirements[i];
                if (req == null || string.IsNullOrEmpty(req.attributeId))
                    continue;
                var widget = GetWidget(shown);
                shown++;
                if (widget?.go == null)
                    continue;

                widget.go.SetActive(true);
                provided.TryGetValue(req.attributeId, out int cur);
                bool met = cur >= req.required;
                if (widget.icon != null)
                    widget.icon.enabled = false;
                if (widget.number != null)
                {
                    FitLabel(widget.number);
                    widget.number.text = $"{ShortName(req.attributeId)}{req.required}";
                    widget.number.color = met
                        ? new Color(0.12f, 0.38f, 0.16f, 1f)
                        : new Color(0.62f, 0.12f, 0.1f, 1f);
                }
            }

            for (int i = shown; i < _widgets.Count; i++)
            {
                if (_widgets[i]?.go != null)
                    _widgets[i].go.SetActive(false);
            }
        }

        // 兼容旧调用
        public void Show(ElementRequirement required, ElementRequirement provided)
        {
            required ??= new ElementRequirement();
            provided ??= new ElementRequirement();
            var reqList = required.ToAttributeList();
            var dict = new Dictionary<string, int>
            {
                ["metal"] = provided.metal,
                ["wood"] = provided.wood,
                ["water"] = provided.water,
                ["fire"] = provided.fire,
                ["earth"] = provided.earth
            };
            Show(reqList, dict);
        }

        void CacheWidgets()
        {
            if (_cached)
                return;
            _cached = true;

            if (rowContainer == null)
            {
                var bg = transform.Find("AttributeBG");
                if (bg != null)
                    rowContainer = bg as RectTransform;
            }

            if (rowContainer == null)
                return;

            for (int i = 0; i < rowContainer.childCount; i++)
            {
                var child = rowContainer.GetChild(i).gameObject;
                if (_template == null)
                    _template = child;
                _widgets.Add(BindWidget(child));
            }
        }

        Widget GetWidget(int index)
        {
            while (_widgets.Count <= index)
            {
                if (_template == null || rowContainer == null)
                    return null;
                var clone = Instantiate(_template, rowContainer);
                clone.name = "Attribute" + (_widgets.Count + 1);
                clone.SetActive(true);
                _widgets.Add(BindWidget(clone));
            }

            return _widgets[index];
        }

        static void FitLabel(TMP_Text label)
        {
            var rt = label.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(2f, 2f);
            rt.offsetMax = new Vector2(-2f, -2f);
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 22;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;
        }

        static string ShortName(string id)
        {
            switch (id)
            {
                case "metal": return "金";
                case "wood": return "木";
                case "water": return "水";
                case "fire": return "火";
                case "earth": return "土";
                default:
                    return string.IsNullOrEmpty(id) ? "?" : id.Substring(0, 1);
            }
        }

        static Widget BindWidget(GameObject go)
        {
            return new Widget
            {
                go = go,
                icon = go.transform.Find("Icon")?.GetComponent<Image>(),
                number = go.transform.Find("Number")?.GetComponent<TMP_Text>()
            };
        }

        public void Clear()
        {
            if (tierText != null)
                tierText.text = string.Empty;
            for (int i = 0; i < _widgets.Count; i++)
            {
                if (_widgets[i]?.go != null)
                    _widgets[i].go.SetActive(false);
            }
            _tier = RequirementTier.None;
        }
    }
}
