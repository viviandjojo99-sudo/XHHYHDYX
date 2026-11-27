using System.Collections.Generic;
using UnityEngine;
using XHHY.Game.Data;

namespace XHHY.Game.Map
{
    /// <summary>
    /// 控制地图上哪些事件钉显示。列表在 Inspector 配置，不写死。
    /// </summary>
    public class MapEventBoard : MonoBehaviour
    {
        static MapEventBoard _instance;

        [System.Serializable]
        public class Entry
        {
            public MapEventMarker marker;
            public EventData eventData;
            public bool visibleOnStart = true;
        }

        public List<Entry> entries = new List<Entry>();

        void Awake() => _instance = this;

        void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        void Start()
        {
            RefreshVisibility();
        }

        public static void SetMarkerActive(string eventId, bool active)
        {
            if (_instance != null)
                _instance.ShowEvent(eventId, active);
        }

        public void RefreshVisibility()
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e == null || e.marker == null)
                    continue;

                if (e.eventData != null)
                    e.marker.eventData = e.eventData;

                e.marker.gameObject.SetActive(e.visibleOnStart);
            }
        }

        public void ShowEvent(string eventId, bool visible)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e?.eventData == null || e.marker == null)
                    continue;
                if (e.eventData.eventId != eventId)
                    continue;
                e.visibleOnStart = visible;
                e.marker.gameObject.SetActive(visible);
            }
        }
    }
}
