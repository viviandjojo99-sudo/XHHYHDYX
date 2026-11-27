using System;
using UnityEngine;
using XHHY.Game.Data;
using XHHY.Game.Runtime;
using XHHY.Game.UI;

namespace XHHY.Game.Settlement
{
    [Serializable]
    public class CmdConsumeCard : SettlementCommand
    {
        public string slotId;
        public string cardId;
        public int amount = 1;

        public override void Execute(SettlementContext ctx)
        {
            var state = ctx.State ?? GameState.Current;
            if (state == null)
                return;

            CardInstance target = null;
            if (!string.IsNullOrEmpty(slotId) && ctx.SlotOccupants.TryGetValue(slotId, out var slotCard))
                target = slotCard;
            else if (!string.IsNullOrEmpty(cardId))
                target = state.FindByCardId(cardId, CardLocation.Hand)
                         ?? state.FindByCardId(cardId, CardLocation.Slot);

            if (target == null)
                return;

            if (target.data != null && target.data.IsStackable)
                state.ConsumeFromStack(target, amount);
            else
                state.MoveCard(target, CardLocation.Consumed);

            ctx.Log.Add($"消耗 {target.data?.title}");
        }

        public override string EditorLabel => $"消耗 槽={slotId} 牌={cardId}";
    }

    [Serializable]
    public class CmdGrantCard : SettlementCommand
    {
        public CardData card;
        public int count = 1;
        public CardLocation location = CardLocation.Hand;

        public override void Execute(SettlementContext ctx)
        {
            var state = ctx.State ?? GameState.Current;
            if (state == null || card == null)
                return;
            state.GrantCard(card, location, count);
            ctx.Log.Add($"获得 {card.title} x{count}");
        }

        public override string EditorLabel => $"获得 {card?.title} x{count}";
    }

    [Serializable]
    public class CmdMoveCard : SettlementCommand
    {
        public string cardId;
        public CardLocation from = CardLocation.Deck;
        public CardLocation to = CardLocation.Hand;

        public override void Execute(SettlementContext ctx)
        {
            var state = ctx.State ?? GameState.Current;
            var card = state?.FindByCardId(cardId, from);
            if (card == null)
                return;
            state.MoveCard(card, to);
            ctx.Log.Add($"移动 {card.data?.title} {from}->{to}");
        }

        public override string EditorLabel => $"移动 {cardId} {from}->{to}";
    }

    [Serializable]
    public class CmdKillCard : SettlementCommand
    {
        public string cardId;

        public override void Execute(SettlementContext ctx)
        {
            var state = ctx.State ?? GameState.Current;
            var card = state?.FindByCardId(cardId);
            if (card == null)
                return;
            state.KillCard(card);
            ctx.Log.Add($"{card.data?.title} 死亡");
        }

        public override string EditorLabel => $"击杀 {cardId}";
    }

    [Serializable]
    public class CmdShowResult : SettlementCommand
    {
        [TextArea(2, 6)] public string text;

        public override void Execute(SettlementContext ctx)
        {
            string msg = !string.IsNullOrEmpty(text) ? text : null;
            ResultPopup.Show(msg ?? "完成");
            ctx.Log.Add("显示结果");
        }

        public override string EditorLabel => "显示结果";
    }

    [Serializable]
    public class CmdSetSwitch : SettlementCommand
    {
        public string switchKey;
        public bool value = true;

        public override void Execute(SettlementContext ctx)
        {
            (ctx.State ?? GameState.Current)?.SetSwitch(switchKey, value);
            ctx.Log.Add($"开关 {switchKey}={value}");
        }

        public override string EditorLabel => $"开关 {switchKey}={value}";
    }

    [Serializable]
    public class CmdAddVariable : SettlementCommand
    {
        public string variableKey;
        public int delta = 1;

        public override void Execute(SettlementContext ctx)
        {
            (ctx.State ?? GameState.Current)?.AddVariable(variableKey, delta);
            ctx.Log.Add($"变量 {variableKey}+{delta}");
        }

        public override string EditorLabel => $"变量 {variableKey}+{delta}";
    }

    [Serializable]
    public class CmdActivateEvent : SettlementCommand
    {
        public string eventId;
        public bool active = true;

        public override void Execute(SettlementContext ctx)
        {
            XHHY.Game.Map.MapEventBoard.SetMarkerActive(eventId, active);
            ctx.Log.Add($"事件标记 {eventId} active={active}");
        }

        public override string EditorLabel => $"激活事件 {eventId}={active}";
    }

    [Serializable]
    public class CmdOpenEvent : SettlementCommand
    {
        public EventData eventData;

        public override void Execute(SettlementContext ctx)
        {
            if (eventData != null)
                EventPanelController.Instance?.Open(eventData);
        }

        public override string EditorLabel => $"打开事件 {eventData?.title}";
    }

    [Serializable]
    public class CmdShowChoices : SettlementCommand
    {
        public string prompt;
        public string[] choices;

        public override void Execute(SettlementContext ctx)
        {
            // 最小实现：弹结果文案；完整选项 UI 后续可接
            ResultPopup.Show(prompt + "\n" + string.Join(" / ", choices ?? Array.Empty<string>()));
        }

        public override string EditorLabel => "显示选项";
    }

    [Serializable]
    public class CmdGrantRecipeOutput : SettlementCommand
    {
        public override void Execute(SettlementContext ctx)
        {
            var state = ctx.State ?? GameState.Current;
            CardInstance recipeCard = null;
            foreach (var kv in ctx.SlotOccupants)
            {
                if (kv.Value?.data?.recipe != null)
                {
                    recipeCard = kv.Value;
                    break;
                }
            }
            if (recipeCard?.data?.recipe == null || state == null)
                return;

            var quality = recipeCard.data.recipe.Evaluate(ctx.ProvidedAttributes);
            if (quality == RecipeQuality.Explode)
            {
                foreach (var kv in ctx.SlotOccupants)
                {
                    if (kv.Value != null && kv.Value != recipeCard)
                        state.MoveCard(kv.Value, CardLocation.Consumed);
                }
                ResultPopup.Show("炸炉！药材尽毁。");
                ctx.Log.Add("炸炉");
                return;
            }

            var output = recipeCard.data.recipe.OutputFor(quality);
            if (output != null)
                state.GrantCard(output, CardLocation.Hand, 1);
            ctx.Log.Add($"炼丹结果 {quality} -> {output?.title}");
        }

        public override string EditorLabel => "按品级发放丹药";
    }

    [Serializable]
    public class CmdMarkEventCompleted : SettlementCommand
    {
        public string eventId;

        public override void Execute(SettlementContext ctx)
        {
            var id = !string.IsNullOrEmpty(eventId) ? eventId : ctx.Event?.eventId;
            (ctx.State ?? GameState.Current)?.MarkEventCompleted(id);
        }

        public override string EditorLabel => $"标记事件完成 {eventId}";
    }
}
