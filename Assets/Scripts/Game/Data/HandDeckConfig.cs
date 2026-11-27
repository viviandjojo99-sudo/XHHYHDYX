using UnityEngine;

namespace XHHY.Game.Data
{
    /// <summary>
    /// 兼容旧资产与菜单；功能与 DeckConfig 相同。
    /// </summary>
    [CreateAssetMenu(fileName = "HandDeckConfig", menuName = "炼丹游戏/起始手牌配置", order = 31)]
    public class HandDeckConfig : DeckConfig
    {
    }
}
