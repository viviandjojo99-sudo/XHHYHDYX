using System;
using XHHY.Game.Data;
using XHHY.Game.Runtime;

namespace XHHY.Game.Settlement
{
    [Serializable]
    public class CondRequirementTier : SettlementCondition
    {
        public RequirementTier tier = RequirementTier.Full;
        public enum Compare { Equal, AtLeast, AtMost }
        public Compare compare = Compare.Equal;

        public override bool Evaluate(SettlementContext ctx)
        {
            int a = (int)ctx.Tier;
            int b = (int)tier;
            switch (compare)
            {
                case Compare.AtLeast: return a >= b;
                case Compare.AtMost: return a <= b;
                default: return a == b;
            }
        }

        public override string EditorLabel => $"分档 {compare} {tier}";
    }

    [Serializable]
    public class CondAttributeTotal : SettlementCondition
    {
        public string attributeId = "fire";
        public enum Op { Greater, GreaterOrEqual, Less, LessOrEqual, Equal }
        public Op op = Op.Greater;
        public int value = 10;

        public override bool Evaluate(SettlementContext ctx)
        {
            ctx.ProvidedAttributes.TryGetValue(attributeId, out int v);
            switch (op)
            {
                case Op.Greater: return v > value;
                case Op.GreaterOrEqual: return v >= value;
                case Op.Less: return v < value;
                case Op.LessOrEqual: return v <= value;
                default: return v == value;
            }
        }

        public override string EditorLabel => $"{attributeId} {op} {value}";
    }

    [Serializable]
    public class CondSlotHasCard : SettlementCondition
    {
        public string slotId;
        public string cardId;

        public override bool Evaluate(SettlementContext ctx)
        {
            if (string.IsNullOrEmpty(slotId) || ctx.SlotOccupants == null)
                return false;
            if (!ctx.SlotOccupants.TryGetValue(slotId, out var inst) || inst?.data == null)
                return false;
            if (string.IsNullOrEmpty(cardId))
                return true;
            return inst.data.cardId == cardId;
        }

        public override string EditorLabel => $"槽 {slotId} 有 {cardId}";
    }

    [Serializable]
    public class CondCardLocation : SettlementCondition
    {
        public string cardId;
        public CardLocation location = CardLocation.Hand;

        public override bool Evaluate(SettlementContext ctx)
        {
            var state = ctx.State ?? GameState.Current;
            if (state == null)
                return false;
            return state.FindByCardId(cardId, location) != null;
        }

        public override string EditorLabel => $"{cardId} 在 {location}";
    }

    [Serializable]
    public class CondCardAlive : SettlementCondition
    {
        public string cardId;
        public bool alive = true;

        public override bool Evaluate(SettlementContext ctx)
        {
            var state = ctx.State ?? GameState.Current;
            if (state == null)
                return false;
            return state.IsAlive(cardId) == alive;
        }

        public override string EditorLabel => $"{cardId} alive={alive}";
    }

    [Serializable]
    public class CondSwitchIs : SettlementCondition
    {
        public string switchKey;
        public bool expected = true;

        public override bool Evaluate(SettlementContext ctx)
        {
            var state = ctx.State ?? GameState.Current;
            return state != null && state.GetSwitch(switchKey) == expected;
        }

        public override string EditorLabel => $"开关 {switchKey}={expected}";
    }

    [Serializable]
    public class CondVariableCompare : SettlementCondition
    {
        public string variableKey;
        public CondAttributeTotal.Op op = CondAttributeTotal.Op.GreaterOrEqual;
        public int value;

        public override bool Evaluate(SettlementContext ctx)
        {
            var state = ctx.State ?? GameState.Current;
            if (state == null)
                return false;
            int v = state.GetVariable(variableKey);
            switch (op)
            {
                case CondAttributeTotal.Op.Greater: return v > value;
                case CondAttributeTotal.Op.GreaterOrEqual: return v >= value;
                case CondAttributeTotal.Op.Less: return v < value;
                case CondAttributeTotal.Op.LessOrEqual: return v <= value;
                default: return v == value;
            }
        }

        public override string EditorLabel => $"变量 {variableKey} {op} {value}";
    }

    [Serializable]
    public class CondEventCompleted : SettlementCondition
    {
        public string eventId;
        public bool completed = true;

        public override bool Evaluate(SettlementContext ctx)
        {
            var state = ctx.State ?? GameState.Current;
            if (state == null)
                return false;
            return state.IsEventCompleted(eventId) == completed;
        }

        public override string EditorLabel => $"事件完成 {eventId}={completed}";
    }

    [Serializable]
    public class CondHasCard : SettlementCondition
    {
        public string cardId;
        public CardLocation location = CardLocation.Hand;

        public override bool Evaluate(SettlementContext ctx)
        {
            var state = ctx.State ?? GameState.Current;
            return state != null && state.FindByCardId(cardId, location) != null;
        }

        public override string EditorLabel => $"有牌 {cardId}@{location}";
    }

    [Serializable]
    public class CondRecipeQuality : SettlementCondition
    {
        public RecipeQuality quality = RecipeQuality.High;

        public override bool Evaluate(SettlementContext ctx)
        {
            CardInstance recipeCard = null;
            foreach (var kv in ctx.SlotOccupants)
            {
                if (kv.Value?.data?.recipe != null)
                {
                    recipeCard = kv.Value;
                    break;
                }
            }
            if (recipeCard?.data?.recipe == null)
                return false;
            var q = recipeCard.data.recipe.Evaluate(ctx.ProvidedAttributes);
            return q == quality;
        }

        public override string EditorLabel => $"炼丹品级={quality}";
    }
}
