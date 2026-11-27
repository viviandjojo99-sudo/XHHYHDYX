using System.Collections.Generic;
using UnityEngine;
using XHHY.Game.Data;
using XHHY.Game.Settlement;

namespace XHHY.Game.Runtime
{
    public class CommonEventRunner : MonoBehaviour
    {
        [SerializeField] List<CommonEventData> commonEvents = new List<CommonEventData>();

        GameState _state;

        public void Bind(GameState state, List<CommonEventData> events)
        {
            Unbind();
            _state = state;
            if (events != null)
                commonEvents = events;
            if (_state == null)
                return;
            _state.OnRoundStarted += HandleRound;
            _state.OnEventCompleted += HandleEventCompleted;
            _state.OnSwitchChanged += HandleSwitch;
            _state.OnCardLocationChanged += HandleCard;
        }

        void OnDestroy() => Unbind();

        void Unbind()
        {
            if (_state == null)
                return;
            _state.OnRoundStarted -= HandleRound;
            _state.OnEventCompleted -= HandleEventCompleted;
            _state.OnSwitchChanged -= HandleSwitch;
            _state.OnCardLocationChanged -= HandleCard;
            _state = null;
        }

        void HandleRound() => Run(CommonEventTrigger.OnRoundStart, null);
        void HandleEventCompleted(string id) => Run(CommonEventTrigger.OnEventCompleted, id);
        void HandleSwitch(string key) => Run(CommonEventTrigger.OnSwitchChanged, key);
        void HandleCard(CardInstance _) => Run(CommonEventTrigger.OnCardChanged, null);

        void Run(CommonEventTrigger trigger, string filter)
        {
            if (commonEvents == null || _state == null)
                return;

            for (int i = 0; i < commonEvents.Count; i++)
            {
                var ev = commonEvents[i];
                if (ev == null || ev.trigger != trigger)
                    continue;
                if (ev.once && ev.consumed)
                    continue;
                if (!string.IsNullOrEmpty(ev.triggerFilter) && !string.IsNullOrEmpty(filter)
                    && ev.triggerFilter != filter)
                    continue;

                var ctx = new SettlementContext { State = _state };
                bool ok = true;
                if (ev.conditions != null)
                {
                    for (int c = 0; c < ev.conditions.Count; c++)
                    {
                        if (ev.conditions[c] != null && !ev.conditions[c].Evaluate(ctx))
                        {
                            ok = false;
                            break;
                        }
                    }
                }
                if (!ok)
                    continue;

                if (ev.commands != null)
                {
                    for (int c = 0; c < ev.commands.Count; c++)
                        ev.commands[c]?.Execute(ctx);
                }
                if (ev.once)
                    ev.consumed = true;
            }
        }
    }
}
