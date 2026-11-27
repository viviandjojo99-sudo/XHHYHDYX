using System.Collections.Generic;
using UnityEngine;
using XHHY.Game.Settlement;

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
        [Tooltip("无 outcomes 时的兜底预览文案")]
        public string previewOutcomeText;

        [Tooltip("旧五行需求，迁移后以 requirements 为准")]
        public ElementRequirement requiredAttributes = new ElementRequirement();

        [Tooltip("可编辑属性需求列表")]
        public List<AttributeRequirement> requirements = new List<AttributeRequirement>();

        public List<EventSlotData> slots = new List<EventSlotData>();

        [Header("出现条件")]
        [SerializeReference] public List<SettlementCondition> appearConditions = new List<SettlementCondition>();
        public bool repeatable = true;
        public bool autoStartOnRound;

        [Header("结果分支（有序；第一个全满足的生效；末条建议无条件兜底）")]
        public List<EventOutcome> outcomes = new List<EventOutcome>();

        public List<AttributeRequirement> ResolveRequirements()
        {
            if (requirements != null && requirements.Count > 0)
                return requirements;
            return requiredAttributes != null
                ? requiredAttributes.ToAttributeList()
                : new List<AttributeRequirement>();
        }

        public bool CanAppear(Runtime.GameState state)
        {
            if (!repeatable && state != null && state.IsEventCompleted(eventId))
                return false;
            if (appearConditions == null || appearConditions.Count == 0)
                return true;
            var ctx = new SettlementContext { State = state, Event = this };
            for (int i = 0; i < appearConditions.Count; i++)
            {
                var c = appearConditions[i];
                if (c != null && !c.Evaluate(ctx))
                    return false;
            }
            return true;
        }
    }
}
