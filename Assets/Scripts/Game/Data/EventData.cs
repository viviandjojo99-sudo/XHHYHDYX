using System.Collections.Generic;
using UnityEngine;

namespace XHHY.Game.Data
{
    [CreateAssetMenu(fileName = "Event_", menuName = "炼丹游戏/事件", order = 20)]
    public class EventData : ScriptableObject
    {
        public string eventId;
        public string title;
        public Sprite background;

        [TextArea(3, 10)]
        [Tooltip("打开事件时的描述")]
        public string openingText;

        [TextArea(2, 6)]
        [Tooltip("放入卡牌后预览的可能结果（确认前）")]
        public string previewOutcomeText;

        [Tooltip("事件需要的五行属性")]
        public ElementRequirement requiredAttributes = new ElementRequirement();

        public List<EventSlotData> slots = new List<EventSlotData>();
    }
}
