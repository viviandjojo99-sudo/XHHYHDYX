using System;
using System.Collections.Generic;
using UnityEngine;
using XHHY.Game.Data;
using XHHY.Game.Runtime;

namespace XHHY.Game.Settlement
{
    public enum PreviewMode
    {
        Show = 0,
        Vague = 1,
        Hidden = 2
    }

    public class SettlementContext
    {
        public GameState State;
        public EventData Event;
        public Dictionary<string, CardInstance> SlotOccupants = new Dictionary<string, CardInstance>();
        public Dictionary<string, int> ProvidedAttributes = new Dictionary<string, int>();
        public RequirementTier Tier;
        public List<string> Log = new List<string>();
    }

    [Serializable]
    public abstract class SettlementCondition
    {
        public abstract bool Evaluate(SettlementContext ctx);
        public virtual string EditorLabel => GetType().Name;
    }

    [Serializable]
    public abstract class SettlementCommand
    {
        public abstract void Execute(SettlementContext ctx);
        public virtual string EditorLabel => GetType().Name;
    }

    [Serializable]
    public class EventOutcome
    {
        public string outcomeId;
        [TextArea(2, 4)] public string previewText;
        [TextArea(2, 6)] public string resultText;
        public PreviewMode previewMode = PreviewMode.Show;

        [SerializeReference] public List<SettlementCondition> conditions = new List<SettlementCondition>();
        [SerializeReference] public List<SettlementCommand> commands = new List<SettlementCommand>();

        public bool Matches(SettlementContext ctx)
        {
            if (conditions == null || conditions.Count == 0)
                return true;
            for (int i = 0; i < conditions.Count; i++)
            {
                var c = conditions[i];
                if (c == null)
                    continue;
                if (!c.Evaluate(ctx))
                    return false;
            }
            return true;
        }

        public void Run(SettlementContext ctx)
        {
            if (commands == null)
                return;
            for (int i = 0; i < commands.Count; i++)
                commands[i]?.Execute(ctx);
        }
    }

    public static class OutcomeResolver
    {
        public static EventOutcome Resolve(EventData data, SettlementContext ctx)
        {
            if (data?.outcomes == null || data.outcomes.Count == 0)
                return null;
            for (int i = 0; i < data.outcomes.Count; i++)
            {
                var o = data.outcomes[i];
                if (o != null && o.Matches(ctx))
                    return o;
            }
            return data.outcomes[data.outcomes.Count - 1];
        }
    }
}
