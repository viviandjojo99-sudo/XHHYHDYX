using System;
using UnityEngine;

namespace XHHY.Game.Data
{
    [Serializable]
    public class EventSlotData
    {
        public string slotId;
        public CardTypeDefinition acceptType;
        public Vector2 anchoredPosition;
        public bool required = true;
    }
}
