using System;
using System.Collections.Generic;
using UnityEngine;

namespace XHHY.Game.Data
{
    public enum CardLocation
    {
        Deck = 0,
        Hand = 1,
        Slot = 2,
        Equipped = 3,
        Consumed = 4,
        Dead = 5
    }

    [Serializable]
    public class CardInstance
    {
        public string uid;
        public CardData data;
        public int count = 1;
        public CardLocation location = CardLocation.Deck;
        public string equippedOnUid;
        public string slotId;
        public string eventId;
        public bool alive = true;
        public List<string> equippedItemUids = new List<string>();
        public List<AttributeValue> modifiers = new List<AttributeValue>();

        public CardData Data => data;
        public int EquippedCount => equippedItemUids != null ? equippedItemUids.Count : 0;

        public static CardInstance Create(CardData card, CardLocation loc, int count = 1)
        {
            return new CardInstance
            {
                uid = Guid.NewGuid().ToString("N"),
                data = card,
                count = Mathf.Max(1, count),
                location = loc,
                alive = true
            };
        }

        public int GetBaseAttribute(string attributeId)
        {
            return data != null ? data.GetAttribute(attributeId) : 0;
        }

        public int GetModifier(string attributeId)
        {
            if (modifiers == null || string.IsNullOrEmpty(attributeId))
                return 0;
            int sum = 0;
            for (int i = 0; i < modifiers.Count; i++)
            {
                var m = modifiers[i];
                if (m != null && m.attributeId == attributeId)
                    sum += m.value;
            }
            return sum;
        }

        public int GetEffectiveAttribute(string attributeId, Func<string, CardInstance> resolve)
        {
            int total = GetBaseAttribute(attributeId) + GetModifier(attributeId);
            if (equippedItemUids == null || resolve == null)
                return total;
            for (int i = 0; i < equippedItemUids.Count; i++)
            {
                var item = resolve(equippedItemUids[i]);
                if (item?.data == null)
                    continue;
                total += item.GetBaseAttribute(attributeId);
                if (item.data.equipModifiers != null)
                {
                    for (int j = 0; j < item.data.equipModifiers.Count; j++)
                    {
                        var m = item.data.equipModifiers[j];
                        if (m != null && m.attributeId == attributeId)
                            total += m.value;
                    }
                }
            }
            return total;
        }

        public void AddModifier(string attributeId, int value)
        {
            if (string.IsNullOrEmpty(attributeId) || value == 0)
                return;
            modifiers ??= new List<AttributeValue>();
            for (int i = 0; i < modifiers.Count; i++)
            {
                if (modifiers[i] != null && modifiers[i].attributeId == attributeId)
                {
                    modifiers[i].value += value;
                    return;
                }
            }
            modifiers.Add(new AttributeValue { attributeId = attributeId, value = value });
        }
    }
}
