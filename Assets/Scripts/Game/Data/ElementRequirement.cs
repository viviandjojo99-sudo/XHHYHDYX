using System;
using System.Collections.Generic;

namespace XHHY.Game.Data
{
    /// <summary>
    /// 兼容旧序列化；新逻辑请用 AttributeRequirement 列表。
    /// </summary>
    [Serializable]
    public class ElementRequirement
    {
        public int metal;
        public int wood;
        public int water;
        public int fire;
        public int earth;

        public List<AttributeRequirement> ToAttributeList()
        {
            var list = new List<AttributeRequirement>();
            if (metal != 0) list.Add(new AttributeRequirement { attributeId = "metal", required = metal });
            if (wood != 0) list.Add(new AttributeRequirement { attributeId = "wood", required = wood });
            if (water != 0) list.Add(new AttributeRequirement { attributeId = "water", required = water });
            if (fire != 0) list.Add(new AttributeRequirement { attributeId = "fire", required = fire });
            if (earth != 0) list.Add(new AttributeRequirement { attributeId = "earth", required = earth });
            return list;
        }

        public static ElementRequirement FromAttributeList(List<AttributeRequirement> list)
        {
            var e = new ElementRequirement();
            if (list == null) return e;
            for (int i = 0; i < list.Count; i++)
            {
                var a = list[i];
                if (a == null) continue;
                switch (a.attributeId)
                {
                    case "metal": e.metal = a.required; break;
                    case "wood": e.wood = a.required; break;
                    case "water": e.water = a.required; break;
                    case "fire": e.fire = a.required; break;
                    case "earth": e.earth = a.required; break;
                }
            }
            return e;
        }
    }
}
