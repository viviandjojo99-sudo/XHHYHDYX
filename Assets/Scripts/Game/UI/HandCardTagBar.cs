using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using XHHY.Game.Data;
using XHHY.Game.Motion;

namespace XHHY.Game.UI
{
    /// <summary>
    /// 「全部」+ 平铺细分类。无牌的分类变灰但不隐藏，保证位置固定。
    /// </summary>
    public class HandCardTagBar : MonoBehaviour
    {
        [SerializeField] CardTypeCatalog catalog;
        [SerializeField] RectTransform tagContainer;
        [SerializeField] HandCardTagItem tagItemPrefab;
        [SerializeField] HandTrayController handTray;
        [SerializeField] bool includeAllButton = true;
        [SerializeField] RectTransform underline;
        [SerializeField] MotionSettings motionSettings;
        [SerializeField] UiSfx uiSfx;

        readonly List<HandCardTagItem> _items = new List<HandCardTagItem>();
        Spring1D _underlineX;
        float _underlineTarget;
        int _selectedIndex;

        void Start() => Rebuild();

        void Update()
        {
            if (underline == null)
                return;
            var motion = motionSettings != null ? motionSettings : MotionSettings.CreateRuntimeDefaults();
            _underlineX.Step(_underlineTarget, motion.tagUnderlineStiffness, motion.tagUnderlineDamping,
                Time.unscaledDeltaTime, motion.substeps);
            var p = underline.anchoredPosition;
            p.x = _underlineX.value;
            underline.anchoredPosition = p;
        }

        public void Rebuild()
        {
            if (tagContainer == null || catalog == null)
                return;

            var types = catalog.GetSortedTypes();
            var needed = types.Count + (includeAllButton ? 1 : 0);
            var existing = new List<HandCardTagItem>(tagContainer.GetComponentsInChildren<HandCardTagItem>(true));

            while (existing.Count < needed && tagItemPrefab != null)
            {
                var item = Instantiate(tagItemPrefab, tagContainer);
                item.gameObject.SetActive(true);
                existing.Add(item);
            }

            _items.Clear();
            int index = 0;
            if (includeAllButton && existing.Count > 0)
            {
                int allCount = handTray != null ? handTray.CountByType(null) : 0;
                existing[index].Setup(null, handTray, allCount, false);
                existing[index].gameObject.SetActive(true);
                _items.Add(existing[index]);
                index++;
            }

            for (int i = 0; i < types.Count && index < existing.Count; i++, index++)
            {
                int count = handTray != null ? handTray.CountByType(types[i]) : 0;
                existing[index].Setup(types[i], handTray, count, count == 0);
                existing[index].gameObject.SetActive(true);
                _items.Add(existing[index]);
            }

            for (; index < existing.Count; index++)
                existing[index].gameObject.SetActive(false);

            SelectIndex(0, true);
        }

        public void SelectIndex(int index, bool snap)
        {
            _selectedIndex = Mathf.Clamp(index, 0, Mathf.Max(0, _items.Count - 1));
            if (_items.Count == 0 || underline == null)
                return;
            var rt = (RectTransform)_items[_selectedIndex].transform;
            _underlineTarget = rt.anchoredPosition.x;
            if (snap)
                _underlineX.Snap(_underlineTarget);
            uiSfx?.PlayToggle();
        }

        public void RefreshCounts()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                var item = _items[i];
                if (item == null)
                    continue;
                int count = handTray != null ? handTray.CountByType(item.Type) : 0;
                item.Setup(item.Type, handTray, count, item.Type != null && count == 0);
            }
        }
    }
}
