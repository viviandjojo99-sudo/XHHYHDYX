using System;
using System.Collections.Generic;
using UnityEngine;

namespace XHHY.Game.Data
{
    [Serializable]
    public class DeckEntry
    {
        public CardData card;
        public int count = 1;
        public CardLocation initialLocation = CardLocation.Hand;
    }

    [CreateAssetMenu(fileName = "DeckConfig", menuName = "炼丹游戏/牌堆配置", order = 30)]
    public class DeckConfig : ScriptableObject
    {
        [Tooltip("开局世界中的全部卡牌（含仅在牌堆的 NPC）")]
        public List<DeckEntry> entries = new List<DeckEntry>();

        [Tooltip("兼容旧配置：全部进手牌")]
        public List<CardData> startingCards = new List<CardData>();

        public List<DeckEntry> ResolveEntries()
        {
            var list = new List<DeckEntry>();
            if (entries != null)
            {
                for (int i = 0; i < entries.Count; i++)
                {
                    if (entries[i]?.card != null)
                        list.Add(entries[i]);
                }
            }

            if (list.Count == 0 && startingCards != null)
            {
                for (int i = 0; i < startingCards.Count; i++)
                {
                    if (startingCards[i] == null)
                        continue;
                    list.Add(new DeckEntry
                    {
                        card = startingCards[i],
                        count = 1,
                        initialLocation = CardLocation.Hand
                    });
                }
            }

            return list;
        }
    }
}
