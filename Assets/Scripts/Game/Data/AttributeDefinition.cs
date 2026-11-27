using UnityEngine;

namespace XHHY.Game.Data
{
    [CreateAssetMenu(fileName = "Attr_", menuName = "炼丹游戏/属性定义", order = 2)]
    public class AttributeDefinition : ScriptableObject
    {
        public string attributeId;
        public string displayName;
        public Sprite icon;
        public int sortOrder;
        [Tooltip("单字简称，用于卡面小块")]
        public string shortLabel;
    }
}
