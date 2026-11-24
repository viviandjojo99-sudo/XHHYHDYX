using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace XHHY.Game.Map
{
    /// <summary>
    /// 主相机平移缩放。指针落在任意 UI Graphic 上时不拖地图、不滚轮缩放。
    /// （New Input System 下 IsPointerOverGameObject() 常失效，改用 RaycastAll。）
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraPanZoom : MonoBehaviour
    {
        [SerializeField] float zoomSpeed = 2f;
        [SerializeField] float minOrthoSize = 2f;
        [SerializeField] float maxOrthoSize = 12f;
        [SerializeField] float panSpeed = 1f;

        static readonly List<RaycastResult> RaycastHits = new List<RaycastResult>(16);

        Camera _cam;
        bool _panning;
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

        public static bool PointerOverUi()
        {
            if (EventSystem.current == null || Mouse.current == null)
                return false;

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };
            RaycastHits.Clear();
            EventSystem.current.RaycastAll(eventData, RaycastHits);
            return RaycastHits.Count > 0;
        }

        void HandleZoom(Mouse mouse)
        {
            float scroll = mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.01f)
                return;

            if (PointerOverUi())
                return;

            _cam.orthographicSize = Mathf.Clamp(
                _cam.orthographicSize - scroll * zoomSpeed * 0.01f,
                minOrthoSize,
                maxOrthoSize);
        }

        void HandlePan(Mouse mouse)
        {
            bool wantPan = mouse.leftButton.isPressed
                           || mouse.rightButton.isPressed
                           || mouse.middleButton.isPressed;

            if (wantPan && !_panning)
            {
                if (PointerOverUi())
                    return;
                _panning = true;
                _lastScreen = mouse.position.ReadValue();
            }
            else if (!wantPan)
            {
                _panning = false;
            }

            if (!_panning)
                return;

            // 拖地图途中滑到 UI 上则立刻停
            if (PointerOverUi())
            {
                _panning = false;
                return;
            }

            Vector2 screen = mouse.position.ReadValue();
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
