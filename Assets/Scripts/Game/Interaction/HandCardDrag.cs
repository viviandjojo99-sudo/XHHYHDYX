using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XHHY.Game.UI;

namespace XHHY.Game.Interaction
{
    /// <summary>
    /// 卡牌拖拽：用 pointer delta 跟随，不改 pivot，避免拿起瞬间跳偏。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class HandCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        public static bool AnyDragging { get; private set; }

        HandCardView _view;
        Canvas _rootCanvas;
        RectTransform _rt;
        CanvasGroup _canvasGroup;
        bool _dragging;

        public void Bind(HandCardView view)
        {
            _view = view;
            _rt = (RectTransform)transform;
            _canvasGroup = GetComponent<CanvasGroup>();
            if (_canvasGroup == null)
                _canvasGroup = gameObject.AddComponent<CanvasGroup>();

            var graphics = GetComponentsInChildren<Graphic>(true);
            for (int i = 0; i < graphics.Length; i++)
            {
                var g = graphics[i];
                if (g == null)
                    continue;
                g.raycastTarget = g.gameObject == gameObject;
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_view == null || _view.IsInSlot)
                return;

            _rt ??= (RectTransform)transform;
            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (_rootCanvas == null)
                return;

            _dragging = true;
            AnyDragging = true;

            var dragLayer = _rootCanvas.transform as RectTransform;
            // 保持世界位置与当前 pivot，避免视觉跳动
            _rt.SetParent(dragLayer, true);
            _rt.SetAsLastSibling();
            _rt.localScale = Vector3.one;

            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.9f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || _rt == null)
                return;

            float scale = _rootCanvas != null ? _rootCanvas.scaleFactor : 1f;
            if (scale < 0.0001f)
                scale = 1f;

            _rt.anchoredPosition += eventData.delta / scale;
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging)
                return;

            _dragging = false;
            AnyDragging = false;
            _canvasGroup.blocksRaycasts = true;
            _canvasGroup.alpha = 1f;

            if (_view != null && _view.IsInSlot)
                return;

            _view?.ReturnToHand();
        }
    }
}
