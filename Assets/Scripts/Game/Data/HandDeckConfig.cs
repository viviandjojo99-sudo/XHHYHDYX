using System.Collections.Generic;
using UnityEngine;

namespace XHHY.Game.Data
{
    [CreateAssetMenu(fileName = "HandDeckConfig", menuName = "炼丹游戏/起始手牌配置", order = 30)]
    public class HandDeckConfig : ScriptableObject
    {
        [Tooltip("开局放进 HandTray 的卡牌列表（可重复）")]
        public List<CardData> startingCards = new List<CardData>();
    }
}
