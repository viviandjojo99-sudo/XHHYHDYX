using System;
using System.Collections.Generic;
using UnityEngine;
using XHHY.Game.Data;

namespace XHHY.Game.Runtime
{
    public class GameState
    {
        public static GameState Current { get; private set; }

        readonly List<CardInstance> _cards = new List<CardInstance>();
        readonly Dictionary<string, bool> _switches = new Dictionary<string, bool>();
        readonly Dictionary<string, int> _variables = new Dictionary<string, int>();
        readonly HashSet<string> _completedEvents = new HashSet<string>();

        public int Round { get; private set; } = 1;
        public IReadOnlyList<CardInstance> Cards => _cards;

        public event Action OnChanged;
        public event Action<string> OnSwitchChanged;
        public event Action<string> OnEventCompleted;
        public event Action OnRoundStarted;
        public event Action<CardInstance> OnCardLocationChanged;

        public static GameState Ensure()
        {
            if (Current == null)
                Current = new GameState();
            return Current;
        }

        public static void ResetAll()
        {
            Current = new GameState();
        }

        public void LoadFromDeck(DeckConfig config)
        {
            _cards.Clear();
            if (config == null)
            {
                Notify();
                return;
            }

            var entries = config.ResolveEntries();
            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                if (e.card != null && e.card.IsStackable && e.count > 1 && e.initialLocation == CardLocation.Hand)
                {
                    _cards.Add(CardInstance.Create(e.card, e.initialLocation, e.count));
                }
                else
                {
                    int n = Mathf.Max(1, e.count);
                    for (int c = 0; c < n; c++)
                        _cards.Add(CardInstance.Create(e.card, e.initialLocation, 1));
                }
            }

            Notify();
        }

        public CardInstance FindByUid(string uid)
        {
            if (string.IsNullOrEmpty(uid))
                return null;
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null && _cards[i].uid == uid)
                    return _cards[i];
            }
            return null;
        }

        public CardInstance FindByCardId(string cardId, CardLocation? location = null)
        {
            if (string.IsNullOrEmpty(cardId))
                return null;
            for (int i = 0; i < _cards.Count; i++)
            {
                var c = _cards[i];
                if (c?.data == null || c.data.cardId != cardId)
                    continue;
                if (location.HasValue && c.location != location.Value)
                    continue;
                return c;
            }
            return null;
        }

        public List<CardInstance> GetByLocation(CardLocation location)
        {
            var list = new List<CardInstance>();
            for (int i = 0; i < _cards.Count; i++)
            {
                if (_cards[i] != null && _cards[i].location == location && _cards[i].alive)
                    list.Add(_cards[i]);
            }
            return list;
        }

        public void MoveCard(CardInstance card, CardLocation location)
        {
            if (card == null)
                return;
            card.location = location;
            if (location != CardLocation.Equipped)
                card.equippedOnUid = null;
            if (location != CardLocation.Slot)
            {
                card.slotId = null;
                card.eventId = null;
            }
            OnCardLocationChanged?.Invoke(card);
            Notify();
        }

        public CardInstance GrantCard(CardData data, CardLocation location, int count = 1)
        {
            if (data == null)
                return null;

            if (data.IsStackable && location == CardLocation.Hand)
            {
                var existing = FindStackInHand(data.cardId);
                if (existing != null)
                {
                    existing.count = Mathf.Min(data.maxStack > 0 ? data.maxStack : 99, existing.count + count);
                    Notify();
                    return existing;
                }
            }

            var inst = CardInstance.Create(data, location, data.IsStackable ? count : 1);
            _cards.Add(inst);
            if (!data.IsStackable && count > 1)
            {
                for (int i = 1; i < count; i++)
                    _cards.Add(CardInstance.Create(data, location, 1));
            }
            Notify();
            return inst;
        }

        public void GrantTakenFromStack(CardInstance taken)
        {
            if (taken == null)
                return;
            _cards.Add(taken);
            Notify();
        }

        public CardInstance FindStackInHand(string cardId)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var c = _cards[i];
                if (c != null && c.location == CardLocation.Hand && c.alive
                    && c.data != null && c.data.cardId == cardId && c.data.IsStackable)
                    return c;
            }
            return null;
        }

        public bool ConsumeFromStack(CardInstance stack, int amount = 1)
        {
            if (stack == null || amount <= 0)
                return false;
            if (stack.count > amount)
            {
                stack.count -= amount;
                Notify();
                return true;
            }

            stack.count = 0;
            stack.location = CardLocation.Consumed;
            Notify();
            return true;
        }

        public void KillCard(CardInstance card)
        {
            if (card == null)
                return;
            card.alive = false;
            card.location = CardLocation.Dead;
            OnCardLocationChanged?.Invoke(card);
            Notify();
        }

        public bool IsAlive(string cardId)
        {
            for (int i = 0; i < _cards.Count; i++)
            {
                var c = _cards[i];
                if (c?.data != null && c.data.cardId == cardId)
                    return c.alive && c.location != CardLocation.Dead && c.location != CardLocation.Consumed;
            }
            return false;
        }

        public bool HasInHand(string cardId)
        {
            return FindByCardId(cardId, CardLocation.Hand) != null;
        }

        public void SetSwitch(string key, bool value)
        {
            if (string.IsNullOrEmpty(key))
                return;
            _switches[key] = value;
            OnSwitchChanged?.Invoke(key);
            Notify();
        }

        public bool GetSwitch(string key, bool defaultValue = false)
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue;
            return _switches.TryGetValue(key, out var v) ? v : defaultValue;
        }

        public void AddVariable(string key, int delta)
        {
            if (string.IsNullOrEmpty(key))
                return;
            _variables.TryGetValue(key, out var v);
            _variables[key] = v + delta;
            Notify();
        }

        public void SetVariable(string key, int value)
        {
            if (string.IsNullOrEmpty(key))
                return;
            _variables[key] = value;
            Notify();
        }

        public int GetVariable(string key, int defaultValue = 0)
        {
            if (string.IsNullOrEmpty(key))
                return defaultValue;
            return _variables.TryGetValue(key, out var v) ? v : defaultValue;
        }

        public void MarkEventCompleted(string eventId)
        {
            if (string.IsNullOrEmpty(eventId))
                return;
            _completedEvents.Add(eventId);
            OnEventCompleted?.Invoke(eventId);
            Notify();
        }

        public bool IsEventCompleted(string eventId)
        {
            return !string.IsNullOrEmpty(eventId) && _completedEvents.Contains(eventId);
        }

        public void AdvanceRound()
        {
            Round++;
            OnRoundStarted?.Invoke();
            Notify();
        }

        public Dictionary<string, int> SumEffectiveAttributes(IEnumerable<CardInstance> instances)
        {
            var dict = new Dictionary<string, int>();
            if (instances == null)
                return dict;

            foreach (var inst in instances)
            {
                if (inst?.data == null)
                    continue;

                var keys = new HashSet<string>();
                void AddKeys(List<AttributeValue> list)
                {
                    if (list == null) return;
                    for (int i = 0; i < list.Count; i++)
                    {
                        if (list[i] != null && !string.IsNullOrEmpty(list[i].attributeId))
                            keys.Add(list[i].attributeId);
                    }
                }

                AddKeys(inst.data.attributes);
                AddKeys(inst.modifiers);
                if (inst.equippedItemUids != null)
                {
                    for (int i = 0; i < inst.equippedItemUids.Count; i++)
                    {
                        var item = FindByUid(inst.equippedItemUids[i]);
                        if (item?.data == null) continue;
                        AddKeys(item.data.attributes);
                        AddKeys(item.data.equipModifiers);
                    }
                }

                foreach (var key in keys)
                {
                    int v = inst.GetEffectiveAttribute(key, FindByUid);
                    dict.TryGetValue(key, out int cur);
                    dict[key] = cur + v;
                }
            }

            return dict;
        }

        public static RequirementTier EvaluateTier(List<AttributeRequirement> requirements, Dictionary<string, int> provided)
        {
            if (requirements == null || requirements.Count == 0)
                return RequirementTier.Full;

            int met = 0;
            int total = 0;
            for (int i = 0; i < requirements.Count; i++)
            {
                var r = requirements[i];
                if (r == null || string.IsNullOrEmpty(r.attributeId) || r.required <= 0)
                    continue;
                total++;
                provided.TryGetValue(r.attributeId, out int v);
                if (v >= r.required)
                    met++;
            }

            if (total == 0)
                return RequirementTier.Full;
            if (met == total)
                return RequirementTier.Full;
            if (met == 0)
                return RequirementTier.Fail;
            return RequirementTier.Partial;
        }

        void Notify() => OnChanged?.Invoke();
    }
}
