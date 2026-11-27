using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using XHHY.Game.Motion;
using XHHY.Game.UI;

namespace XHHY.Game.Interaction
{
    /// <summary>
    /// 拖拽时记下抓取点，牌跟着指针走，不改 pivot。拖拽期间手牌布局冻结。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class HandCardDrag : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler, IPointerDownHandler
    {
        public static bool AnyDragging { get; private set; }

        const float ClickSlop = 8f;

        HandCardView _view;
        Canvas _rootCanvas;
        RectTransform _rt;
        RectTransform _dragLayer;
        CanvasGroup _canvasGroup;
        bool _dragging;
        Vector2 _grabOffset;
        Vector2 _lastPointerLocal;
        Vector2 _pressScreen;
        float _lastVx;
        MotionSettings _motion;
        UiSfx _sfx;
        bool _movedBeyondClick;
        bool _pressed;
        bool _liftedFromSlot;

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

        public void SetMotion(MotionSettings motion, UiSfx sfx)
        {
            _motion = motion;
            _sfx = sfx;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            _pressed = true;
            _movedBeyondClick = false;
            _pressScreen = eventData.position;
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (_view == null)
                return;

            _rt ??= (RectTransform)transform;
            _rootCanvas = GetComponentInParent<Canvas>()?.rootCanvas;
            if (_rootCanvas == null)
                return;

            _liftedFromSlot = _view.IsInSlot;
            if (_liftedFromSlot)
                _view.LiftFromSlot();

            _dragging = true;
            AnyDragging = true;
            _movedBeyondClick = true;
            _pressScreen = eventData.position;
            _view.HomeTray?.NotifyDragBegan(_view);
            _sfx?.PlayGrab();

            _dragLayer = _rootCanvas.transform as RectTransform;
            _rt.SetParent(_dragLayer, true);
            _rt.SetAsLastSibling();

            if (ScreenToDragLocal(eventData, out var pointerLocal))
            {
                _grabOffset = _rt.anchoredPosition - pointerLocal;
                _lastPointerLocal = pointerLocal;
            }
            else
            {
                _grabOffset = Vector2.zero;
                _lastPointerLocal = _rt.anchoredPosition;
            }

            _lastVx = 0f;
            _canvasGroup.blocksRaycasts = false;
            _canvasGroup.alpha = 0.92f;
        }

        public void OnDrag(PointerEventData eventData)
        {
            if (!_dragging || _rt == null || _dragLayer == null)
                return;

            if (Vector2.Distance(_pressScreen, eventData.position) > ClickSlop)
                _movedBeyondClick = true;

            if (!ScreenToDragLocal(eventData, out var pointerLocal))
                return;

            float dt = Mathf.Max(0.0001f, Time.unscaledDeltaTime);
            float vx = (pointerLocal.x - _lastPointerLocal.x) / dt;
            _lastVx = Mathf.Lerp(_lastVx, vx, 0.35f);
            _lastPointerLocal = pointerLocal;
            _rt.anchoredPosition = pointerLocal + _grabOffset;

            var settings = _motion != null ? _motion : MotionSettings.CreateRuntimeDefaults();
            float tilt = Mathf.Clamp(_lastVx * settings.tiltPerVelocity, -settings.maxTiltDeg, settings.maxTiltDeg);
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(Mathf.Abs(_lastVx) / 800f));
            _rt.localRotation = Quaternion.Euler(0f, 0f, tilt * t);
            _view?.SetDragTiltTarget(tilt * t);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!_dragging)
                return;

            _dragging = false;
            AnyDragging = false;
            _view?.SetDragTiltTarget(0f);

            if (_view != null && !_view.IsInSlot)
            {
                var slot = _liftedFromSlot
                    ? EventCardSlot.FindUnderPointer(eventData.position, _view.Data)
                    : EventCardSlot.FindForCard(_rt, eventData.position, _view.Data);
                if (slot != null)
                {
                    slot.Place(_view);
                    _sfx?.PlayDock();
                }
            }

            _liftedFromSlot = false;

            if (_canvasGroup != null)
            {
                _canvasGroup.blocksRaycasts = true;
                _canvasGroup.alpha = 1f;
            }

            if (_view != null && _view.IsInSlot)
            {
                _view.HomeTray?.NotifyDragEnded();
                return;
            }

            _view?.ReturnToHand();
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (!_pressed || _dragging || _movedBeyondClick)
            {
                _movedBeyondClick = false;
                _pressed = false;
                return;
            }

            _pressed = false;
            if (_view == null || _view.IsInSlot)
                return;

            if (Vector2.Distance(_pressScreen, eventData.position) > ClickSlop)
                return;

            CardInformationPanel.Open(_view);
        }

        bool ScreenToDragLocal(PointerEventData eventData, out Vector2 local)
        {
            local = Vector2.zero;
            if (_dragLayer == null)
                return false;

            Camera cam = null;
            if (_rootCanvas != null && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                cam = _rootCanvas.worldCamera;

            return RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _dragLayer, eventData.position, cam, out local);
        }
    }
}
