using UnityEngine;

namespace XHHY.Game.Data
{
    [CreateAssetMenu(fileName = "CardType_", menuName = "炼丹游戏/卡牌类型", order = 0)]
    public class CardTypeDefinition : ScriptableObject
    {
        [Tooltip("稳定键，如 renwu；代码用这个比对，不要改来改去")]
        public string typeId;

        [Tooltip("界面显示名，如 人物 / 草药 / 矿石")]
        public string displayName;

        [Tooltip("粗分组，如 herb；卡槽可用 acceptGroup 接受整组")]
        public string groupId;

        public Sprite icon;

        public int sortOrder;

        [Tooltip("该类型默认是否可堆叠")]
        public bool defaultStackable;

        void OnValidate()
        {
            if (string.IsNullOrEmpty(groupId) && !string.IsNullOrEmpty(typeId))
                groupId = InferGroup(typeId);
        }

        static string InferGroup(string typeId)
        {
            switch (typeId)
            {
                case "renwu": return "character";
                case "yaocai":
                case "caoyao":
                case "kuangshi":
                case "lingshou": return "herb";
                case "zhuangbei": return "equip";
                case "peifang": return "recipe";
                case "danyao": return "pill";
                case "qingbao": return "intel";
                case "shiwu": return "food";
                case "zhongzi": return "seed";
                case "gongju": return "tool";
                case "danlu": return "furnace";
                case "shuji": return "book";
                default: return typeId;
            }
        }

        static bool InferStackable(string typeId)
        {
            switch (typeId)
            {
                case "yaocai":
                case "caoyao":
                case "kuangshi":
                case "danyao":
                case "shiwu":
                case "zhongzi":
                    return true;
                default:
                    return false;
            }
        }
    }
}
