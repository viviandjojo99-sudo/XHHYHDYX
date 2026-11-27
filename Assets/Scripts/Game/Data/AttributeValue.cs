using System;

namespace XHHY.Game.Data
{
    [Serializable]
    public class AttributeValue
    {
        public string attributeId;
        public int value;
    }

    [Serializable]
    public class AttributeRequirement
    {
        public string attributeId;
        public int required;
    }

    public enum RequirementTier
    {
        None = 0,
        Fail = 1,
        Partial = 2,
        Full = 3
    }
}
