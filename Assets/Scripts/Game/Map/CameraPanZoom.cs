using UnityEngine;
using UnityEngine.InputSystem;

namespace XHHY.Game.Map
{
    /// <summary>
    /// 主相机平移缩放。按下时若不在 UI 上才开始拖；途中进入 UI 则停，直到松开。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraPanZoom : MonoBehaviour
    {
        [SerializeField] float zoomSpeed = 2.8f;
        [SerializeField] float minOrthoSize = 2f;
        [SerializeField] float maxOrthoSize = 12f;
        [SerializeField] float panSpeed = 1f;

        Camera _cam;
        bool _panning;
        bool _blockedUntilRelease;
        Vector2 _lastScreen;

        void Awake()
        {
            _cam = GetComponent<Camera>();
            if (!_cam.orthographic)
                _cam.orthographic = true;
        }

        void Update()
        {
            var mouse = Mouse.current;
            if (mouse == null)
                return;

            HandleZoom(mouse);
            HandlePan(mouse);
        }

        void HandleZoom(Mouse mouse)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f)
                return;

            if (UiPointerGate.IsOverUi())
                return;

            // 新输入系统有的鼠标一格是 1，有的是 120。统一成「一格」。
            float notches = Mathf.Abs(scroll) > 10f ? scroll / 120f : scroll;
            _cam.orthographicSize = Mathf.Clamp(
                _cam.orthographicSize - notches * zoomSpeed,
                minOrthoSize,
                maxOrthoSize);
        }

        void HandlePan(Mouse mouse)
        {
            bool pressed = mouse.leftButton.isPressed
                           || mouse.rightButton.isPressed
                           || mouse.middleButton.isPressed;
            bool began = mouse.leftButton.wasPressedThisFrame
                         || mouse.rightButton.wasPressedThisFrame
                         || mouse.middleButton.wasPressedThisFrame;

            if (!pressed)
            {
                _panning = false;
                _blockedUntilRelease = false;
                return;
            }

            if (began)
                _blockedUntilRelease = UiPointerGate.IsOverUi();

            if (_blockedUntilRelease)
            {
                _panning = false;
                return;
            }

            if (UiPointerGate.IsOverUi())
            {
                _panning = false;
                _blockedUntilRelease = true;
                return;
            }

            Vector2 screen = mouse.position.ReadValue();
            if (!_panning)
            {
                _panning = true;
                _lastScreen = screen;
                return;
            }

            Vector2 delta = screen - _lastScreen;
            _lastScreen = screen;

            float worldPerPixel = (2f * _cam.orthographicSize) / Mathf.Max(1, Screen.height);
            transform.position += new Vector3(
                -delta.x * worldPerPixel * panSpeed,
                -delta.y * worldPerPixel * panSpeed,
                0f);
        }
    }
}
