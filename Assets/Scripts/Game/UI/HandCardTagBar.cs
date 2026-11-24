using System.Collections.Generic;
using UnityEngine;
using XHHY.Game.Data;

namespace XHHY.Game.UI
{
    /// <summary>
    /// 根据 CardTypeCatalog 填充 HandCardTag 区域。
    /// 优先复用已有子物体；不够再实例化 prefab。
    /// </summary>
    public class HandCardTagBar : MonoBehaviour
    {
        [SerializeField] CardTypeCatalog catalog;
        [SerializeField] RectTransform tagContainer;
        [SerializeField] HandCardTagItem tagItemPrefab;
        [SerializeField] HandTrayController handTray;
        [SerializeField] bool includeAllButton = true;

        void Start()
        {
            Rebuild();
        }

        public void Rebuild()
        {
            if (tagContainer == null || catalog == null)
                return;

            var types = catalog.GetSortedTypes();
            var needed = types.Count + (includeAllButton ? 1 : 0);

            var existing = new List<HandCardTagItem>(tagContainer.GetComponentsInChildren<HandCardTagItem>(true));

            // 补齐数量
            while (existing.Count < needed && tagItemPrefab != null)
            {
                var item = Instantiate(tagItemPrefab, tagContainer);
                item.gameObject.SetActive(true);
                existing.Add(item);
            }

            int index = 0;
            if (includeAllButton && existing.Count > 0)
            {
                existing[index].Setup(null, handTray);
                existing[index].gameObject.SetActive(true);
                index++;
            }

            for (int i = 0; i < types.Count && index < existing.Count; i++, index++)
            {
                existing[index].Setup(types[i], handTray);
                existing[index].gameObject.SetActive(true);
            }

            for (; index < existing.Count; index++)
                existing[index].gameObject.SetActive(false);
        }
    }
}
