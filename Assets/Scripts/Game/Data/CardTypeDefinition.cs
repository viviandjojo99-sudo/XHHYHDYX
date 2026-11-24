using UnityEngine;

namespace XHHY.Game.Data
{
    [CreateAssetMenu(fileName = "CardType_", menuName = "炼丹游戏/卡牌类型", order = 0)]
    public class CardTypeDefinition : ScriptableObject
    {
        [Tooltip("稳定键，如 renwu；代码用这个比对，不要改来改去")]
        public string typeId;

        [Tooltip("界面显示名，如 人物")]
        public string displayName;

        public Sprite icon;

        public int sortOrder;
    }
}
