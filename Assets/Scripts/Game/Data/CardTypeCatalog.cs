using System.Collections.Generic;
using UnityEngine;

namespace XHHY.Game.Data
{
    [CreateAssetMenu(fileName = "CardTypeCatalog", menuName = "炼丹游戏/卡牌类型总表", order = 1)]
    public class CardTypeCatalog : ScriptableObject
    {
        public List<CardTypeDefinition> types = new List<CardTypeDefinition>();

        public CardTypeDefinition FindById(string typeId)
        {
            if (string.IsNullOrEmpty(typeId) || types == null)
                return null;

            for (int i = 0; i < types.Count; i++)
            {
                var t = types[i];
                if (t != null && t.typeId == typeId)
                    return t;
            }

            return null;
        }

        public List<CardTypeDefinition> GetSortedTypes()
        {
            var list = new List<CardTypeDefinition>();
            if (types == null)
                return list;

            for (int i = 0; i < types.Count; i++)
            {
                if (types[i] != null)
                    list.Add(types[i]);
            }

            list.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
            return list;
        }
    }
}
