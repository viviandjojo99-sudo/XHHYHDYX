#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;
using XHHY.Game.Data;

namespace XHHY.Game.EditorTools
{
    public static class CreateDefaultGameData
    {
        const string TypesFolder = "Assets/Data/CardTypes";
        const string CardsFolder = "Assets/Data/Cards";
        const string EventsFolder = "Assets/Data/Events";

        static readonly (string id, string name, int order)[] DefaultTypes =
        {
            ("renwu", "人物", 0),
            ("yaocai", "药材", 1),
            ("zhuangbei", "装备", 2),
            ("peifang", "配方", 3),
            ("danyao", "丹药", 4),
            ("qingbao", "情报", 5),
            ("shiwu", "食物", 6),
            ("zhongzi", "种子", 7),
            ("gongju", "工具", 8),
            ("shuji", "书籍", 9),
        };

        [MenuItem("炼丹游戏/生成默认数据资产")]
        public static void CreateAll()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(TypesFolder);
            EnsureFolder(CardsFolder);
            EnsureFolder(EventsFolder);

            var catalog = LoadOrCreate<CardTypeCatalog>(TypesFolder + "/CardTypeCatalog.asset");
            catalog.types.Clear();

            CardTypeDefinition renwu = null;
            CardTypeDefinition yaocai = null;

            foreach (var (id, name, order) in DefaultTypes)
            {
                var path = $"{TypesFolder}/CardType_{id}.asset";
                var typeAsset = LoadOrCreate<CardTypeDefinition>(path);
                typeAsset.typeId = id;
                typeAsset.displayName = name;
                typeAsset.sortOrder = order;
                EditorUtility.SetDirty(typeAsset);
                catalog.types.Add(typeAsset);

                if (id == "renwu") renwu = typeAsset;
                if (id == "yaocai") yaocai = typeAsset;
            }

            EditorUtility.SetDirty(catalog);

            var hero = LoadOrCreate<CardData>(CardsFolder + "/Card_采药人.asset");
            hero.cardId = "card_caiyaoren";
            hero.title = "采药人";
            hero.description = "擅长采药的人物卡。";
            hero.cardType = renwu;
            hero.skillPoints = 2;
            hero.elementWood = 2;
            EditorUtility.SetDirty(hero);

            var herb = LoadOrCreate<CardData>(CardsFolder + "/Card_一级药材A.asset");
            herb.cardId = "card_herb_1a";
            herb.title = "一级药材A";
            herb.description = "常见草药。";
            herb.cardType = yaocai;
            herb.elementWood = 2;
            EditorUtility.SetDirty(herb);

            var shovel = LoadOrCreate<CardData>(CardsFolder + "/Card_铁铲.asset");
            shovel.cardId = "card_tieshan";
            shovel.title = "铁铲";
            shovel.description = "挖掘用工具。";
            shovel.cardType = catalog.FindById("gongju");
            EditorUtility.SetDirty(shovel);

            var evt = LoadOrCreate<EventData>(EventsFolder + "/Event_上山采药.asset");
            evt.eventId = "event_shangshan_caiyao";
            evt.title = "上山采药";
            evt.openingText = "山路边有一丛药草。需要合适的人来采，也可以带上工具。";
            evt.previewOutcomeText = "若属性足够，你将采得草药与种子；不足则可能空手而归，甚至受伤。";
            evt.requiredAttributes = new ElementRequirement
            {
                metal = 0, wood = 2, water = 0, fire = 0, earth = 1
            };
            evt.slots.Clear();
            evt.slots.Add(new EventSlotData
            {
                slotId = "slot_person",
                acceptType = renwu,
                anchoredPosition = new Vector2(-200f, 0f),
                required = true
            });
            evt.slots.Add(new EventSlotData
            {
                slotId = "slot_tool",
                acceptType = catalog.FindById("gongju"),
                anchoredPosition = new Vector2(0f, 0f),
                required = false
            });
            EditorUtility.SetDirty(evt);

            var deck = LoadOrCreate<HandDeckConfig>(CardsFolder + "/HandDeckConfig.asset");
            deck.startingCards.Clear();
            deck.startingCards.Add(hero);
            deck.startingCards.Add(herb);
            deck.startingCards.Add(shovel);
            EditorUtility.SetDirty(deck);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("已生成默认卡牌类型、示例卡牌、示例事件与起始手牌配置。可在 Assets/Data 下继续可视化编辑。");
        }

        static void EnsureFolder(string path)
        {
            path = path.Replace("\\", "/");
            if (AssetDatabase.IsValidFolder(path))
                return;

            var parent = Path.GetDirectoryName(path)?.Replace("\\", "/");
            var name = Path.GetFileName(path);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
                return;

            if (!AssetDatabase.IsValidFolder(parent))
                EnsureFolder(parent);

            AssetDatabase.CreateFolder(parent, name);
        }

        static T LoadOrCreate<T>(string path) where T : ScriptableObject
        {
            var existing = AssetDatabase.LoadAssetAtPath<T>(path);
            if (existing != null)
                return existing;

            var created = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(created, path);
            return created;
        }
    }
}
#endif
