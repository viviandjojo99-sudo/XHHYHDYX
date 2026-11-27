using UnityEngine;

namespace XHHY.Game.UI
{
    [CreateAssetMenu(fileName = "HandLayoutSettings", menuName = "炼丹游戏/手牌布局", order = 31)]
    public class HandLayoutSettings : ScriptableObject
    {
        [Tooltip("平铺时，相邻两张牌中心距 = 牌宽 + gap")]
        public float gap = 16f;

        public float fallbackCardWidth = 140f;
        public float baseY = 10f;

        [Tooltip("悬停时邻居被推开的距离")]
        public float push = 72f;
        public float falloff1 = 1f;
        public float falloff2 = 0.5f;
        public float falloff3 = 0.2f;

        public float hoverLift = 46f;
        public float hoverScale = 1.12f;

        [Tooltip("靠向目标位置的时间，越小越跟手")]
        public float dampTime = 0.09f;

        [Tooltip("指针要越过相邻牌中心的这段比例，才切换悬停")]
        [Range(0.5f, 0.9f)]
        public float hysteresis = 0.6f;
    }
}
