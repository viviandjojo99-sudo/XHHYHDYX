using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace XHHY.Game.Map
{
    /// <summary>
    /// 用 RaycastAll 判断指针是否在 UI 上。不要用 IsPointerOverGameObject。
    /// </summary>
    public static class UiPointerGate
    {
        static readonly List<RaycastResult> Hits = new List<RaycastResult>(16);

        public static bool IsOverUi()
        {
            if (EventSystem.current == null || Mouse.current == null)
                return false;

            var eventData = new PointerEventData(EventSystem.current)
            {
                position = Mouse.current.position.ReadValue()
            };
            Hits.Clear();
            EventSystem.current.RaycastAll(eventData, Hits);
            return Hits.Count > 0;
        }
    }
}
