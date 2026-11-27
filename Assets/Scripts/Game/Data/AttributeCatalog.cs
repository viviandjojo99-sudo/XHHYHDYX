using System.Collections.Generic;
using UnityEngine;

namespace XHHY.Game.Data
{
    [CreateAssetMenu(fileName = "AttributeCatalog", menuName = "炼丹游戏/属性总表", order = 3)]
    public class AttributeCatalog : ScriptableObject
    {
        public List<AttributeDefinition> attributes = new List<AttributeDefinition>();

        public AttributeDefinition FindById(string attributeId)
        {
            if (string.IsNullOrEmpty(attributeId) || attributes == null)
                return null;
            for (int i = 0; i < attributes.Count; i++)
            {
                var a = attributes[i];
                if (a != null && a.attributeId == attributeId)
                    return a;
            }
            return null;
        }

        public List<AttributeDefinition> GetSorted()
        {
            var list = new List<AttributeDefinition>();
            if (attributes == null)
                return list;
            for (int i = 0; i < attributes.Count; i++)
            {
                if (attributes[i] != null)
                    list.Add(attributes[i]);
            }
            list.Sort((a, b) => a.sortOrder.CompareTo(b.sortOrder));
            return list;
        }
    }
}
