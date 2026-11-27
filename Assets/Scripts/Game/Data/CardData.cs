using System.Collections.Generic;
using UnityEngine;

namespace XHHY.Game.Data
{
    [CreateAssetMenu(fileName = "Card_", menuName = "炼丹游戏/卡牌", order = 10)]
    public class CardData : ScriptableObject
    {
        public string cardId;
        public string title;

        [TextArea(2, 5)]
        public string description;

        public Sprite icon;
        public CardTypeDefinition cardType;

        [Header("属性（可编辑目录）")]
        public List<AttributeValue> attributes = new List<AttributeValue>();

        [Header("堆叠")]
        public bool stackable;
        public int maxStack = 99;

        public bool IsStackable => stackable || (cardType != null && cardType.defaultStackable);

        [Header("标签")]
        public List<string> tags = new List<string>();

        [Header("人物扩展")]
        public int skillPoints;
        public List<string> equipSlots = new List<string>();

        [Header("食物")]
        public List<AttributeValue> onEat = new List<AttributeValue>();
        public int eatDurationRounds;

        [Header("装备")]
        public List<AttributeValue> equipModifiers = new List<AttributeValue>();
        public string equipSlotId;

        [Header("丹方（P10）")]
        public RecipeData recipe;

        // 兼容旧资产迁移字段（菜单迁移后可清空）
        [HideInInspector] public int elementMetal;
        [HideInInspector] public int elementWood;
        [HideInInspector] public int elementWater;
        [HideInInspector] public int elementFire;
        [HideInInspector] public int elementEarth;

        public int GetAttribute(string attributeId)
        {
            if (string.IsNullOrEmpty(attributeId))
                return 0;
            if (attributes != null)
            {
                for (int i = 0; i < attributes.Count; i++)
                {
                    var a = attributes[i];
                    if (a != null && a.attributeId == attributeId)
                        return a.value;
                }
            }

            // 兼容未迁移的旧五行字段
            switch (attributeId)
            {
                case "metal": return elementMetal;
                case "wood": return elementWood;
                case "water": return elementWater;
                case "fire": return elementFire;
                case "earth": return elementEarth;
                default: return 0;
            }
        }

        public void SetAttribute(string attributeId, int value)
        {
            attributes ??= new List<AttributeValue>();
            for (int i = 0; i < attributes.Count; i++)
            {
                if (attributes[i] != null && attributes[i].attributeId == attributeId)
                {
                    attributes[i].value = value;
                    return;
                }
            }
            attributes.Add(new AttributeValue { attributeId = attributeId, value = value });
        }

        public List<AttributeValue> GetSortedAttributes()
        {
            var list = new List<AttributeValue>();
            if (attributes != null)
            {
                for (int i = 0; i < attributes.Count; i++)
                {
                    if (attributes[i] != null && attributes[i].value != 0)
                        list.Add(attributes[i]);
                }
            }

            if (list.Count == 0)
            {
                void AddLegacy(string id, int v)
                {
                    if (v != 0)
                        list.Add(new AttributeValue { attributeId = id, value = v });
                }
                AddLegacy("metal", elementMetal);
                AddLegacy("wood", elementWood);
                AddLegacy("water", elementWater);
                AddLegacy("fire", elementFire);
                AddLegacy("earth", elementEarth);
            }

            list.Sort((a, b) => b.value.CompareTo(a.value));
            return list;
        }
    }
}
