using UnityEngine;
using UnityEngine.EventSystems;
using XHHY.Game.Data;
using XHHY.Game.UI;

namespace XHHY.Game.Map
{
    /// <summary>
    /// 挂在 EventMarkers 下的世界事件钉上，Inspector 指定 EventData。
    /// </summary>
    public class MapEventMarker : MonoBehaviour, IPointerClickHandler
    {
        public EventData eventData;

        public void OnPointerClick(PointerEventData eventDataPointer)
        {
            if (eventData == null)
            {
                Debug.LogWarning($"[{name}] 未指定 EventData，无法打开事件。", this);
                return;
            }

            if (EventPanelController.Instance == null)
            {
                Debug.LogWarning("场景中没有 EventPanelController。", this);
                return;
            }

            EventPanelController.Instance.Open(eventData);
        }

        void Reset()
        {
            EnsureCollider();
        }

        void Awake()
        {
            EnsureCollider();
        }

        void EnsureCollider()
        {
            if (GetComponent<Collider2D>() != null)
                return;

            var box = gameObject.AddComponent<BoxCollider2D>();
            var renderers = GetComponentsInChildren<SpriteRenderer>();
            if (renderers != null && renderers.Length > 0)
            {
                var bounds = renderers[0].bounds;
                for (int i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                box.size = transform.InverseTransformVector(bounds.size);
                box.offset = transform.InverseTransformPoint(bounds.center);
            }
            else
            {
                box.size = Vector2.one;
            }
        }
    }
}
