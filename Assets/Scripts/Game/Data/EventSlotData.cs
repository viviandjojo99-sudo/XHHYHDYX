using System;
using System.Collections.Generic;
using UnityEngine;

namespace XHHY.Game.Data
{
    [Serializable]
    public class EventSlotData
    {
        public string slotId;

        [Tooltip("兼容旧数据：单一接受类型")]
        public CardTypeDefinition acceptType;

        [Tooltip("可接受的多个细分类")]
        public List<CardTypeDefinition> acceptTypes = new List<CardTypeDefinition>();

        [Tooltip("按 groupId 接受整组，如 herb")]
        public string acceptGroup;

        public Vector2 anchoredPosition;
        public bool required = true;

        public bool Accepts(CardData card)
        {
            if (card?.cardType == null)
                return false;

            // 人物装备槽：卡牌声明的 equipSlotId 与槽位 id 一致即可
            if (!string.IsNullOrEmpty(slotId) && !string.IsNullOrEmpty(card.equipSlotId)
                && card.equipSlotId == slotId)
                return true;

            string typeId = card.cardType.typeId;
            string groupId = card.cardType.groupId;

            if (!string.IsNullOrEmpty(acceptGroup) && groupId == acceptGroup)
                return true;

            if (acceptTypes != null)
            {
                for (int i = 0; i < acceptTypes.Count; i++)
                {
                    var t = acceptTypes[i];
                    if (t != null && t.typeId == typeId)
                        return true;
                }
            }

            if (acceptType != null && acceptType.typeId == typeId)
                return true;

            return false;
        }

        public CardTypeDefinition PrimaryAcceptType
        {
            get
            {
                if (acceptType != null)
                    return acceptType;
                if (acceptTypes != null)
                {
                    for (int i = 0; i < acceptTypes.Count; i++)
                    {
                        if (acceptTypes[i] != null)
                            return acceptTypes[i];
                    }
                }
                return null;
            }
        }
    }
}
