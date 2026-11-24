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

        [Header("人物扩展（非人物可留空）")]
        public int skillPoints;
        public int elementMetal;
        public int elementWood;
        public int elementWater;
        public int elementFire;
        public int elementEarth;
    }
}
