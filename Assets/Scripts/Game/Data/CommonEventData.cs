using System.Collections.Generic;
using UnityEngine;
using XHHY.Game.Settlement;

namespace XHHY.Game.Data
{
    public enum CommonEventTrigger
    {
        OnRoundStart = 0,
        OnEventCompleted = 1,
        OnSwitchChanged = 2,
        OnCardChanged = 3
    }

    [CreateAssetMenu(fileName = "CommonEvent_", menuName = "炼丹游戏/公共事件", order = 25)]
    public class CommonEventData : ScriptableObject
    {
        public string commonEventId;
        public string title;
        public CommonEventTrigger trigger = CommonEventTrigger.OnRoundStart;
        public string triggerFilter;
        public bool once;
        [SerializeReference] public List<SettlementCondition> conditions = new List<SettlementCondition>();
        [SerializeReference] public List<SettlementCommand> commands = new List<SettlementCommand>();
        [HideInInspector] public bool consumed;
    }
}
