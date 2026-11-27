#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using XHHY.Game.Data;
using XHHY.Game.Motion;
using XHHY.Game.Settlement;

namespace XHHY.Game.EditorTools
{
    public static class CreateDefaultGameData
    {
        const string TypesFolder = "Assets/Data/CardTypes";
        const string CardsFolder = "Assets/Data/Cards";
        const string EventsFolder = "Assets/Data/Events";
        const string AttrFolder = "Assets/Data/Attributes";
        const string CommonFolder = "Assets/Data/CommonEvents";

        static readonly (string id, string name, int order, string group, bool stack)[] DefaultTypes =
        {
            ("renwu", "人物", 0, "character", false),
            ("caoyao", "草药", 1, "herb", true),
            ("kuangshi", "矿石", 2, "herb", true),
            ("lingshou", "灵兽", 3, "herb", false),
            ("yaocai", "药材", 4, "herb", true),
            ("zhuangbei", "装备", 5, "equip", false),
            ("peifang", "丹方", 6, "recipe", false),
            ("danyao", "丹药", 7, "pill", true),
            ("qingbao", "情报", 8, "intel", false),
            ("shiwu", "食物", 9, "food", true),
            ("zhongzi", "种子", 10, "seed", true),
            ("gongju", "工具", 11, "tool", false),
            ("danlu", "丹炉", 12, "furnace", false),
            ("shuji", "书籍", 13, "book", false),
        };

        static readonly (string id, string name, string shortLabel, int order)[] DefaultAttrs =
        {
            ("metal", "金", "金", 0),
            ("wood", "木", "木", 1),
            ("water", "水", "水", 2),
            ("fire", "火", "火", 3),
            ("earth", "土", "土", 4),
        };

        [MenuItem("炼丹游戏/生成默认数据资产")]
        public static void CreateAll()
        {
            EnsureFolder("Assets/Data");
            EnsureFolder(TypesFolder);
            EnsureFolder(CardsFolder);
            EnsureFolder(EventsFolder);
            EnsureFolder(AttrFolder);
            EnsureFolder(CommonFolder);

            var motion = LoadOrCreate<MotionSettings>("Assets/Data/MotionSettings.asset");
            EditorUtility.SetDirty(motion);
            LoadOrCreate<UiSfx>("Assets/Data/UiSfx.asset");

            var attrCatalog = LoadOrCreate<AttributeCatalog>(AttrFolder + "/AttributeCatalog.asset");
            attrCatalog.attributes.Clear();
            foreach (var (id, name, shortLabel, order) in DefaultAttrs)
            {
                var path = $"{AttrFolder}/Attr_{id}.asset";
                var a = LoadOrCreate<AttributeDefinition>(path);
                a.attributeId = id;
                a.displayName = name;
                a.shortLabel = shortLabel;
                a.sortOrder = order;
                EditorUtility.SetDirty(a);
                attrCatalog.attributes.Add(a);
            }
            EditorUtility.SetDirty(attrCatalog);

            var catalog = LoadOrCreate<CardTypeCatalog>(TypesFolder + "/CardTypeCatalog.asset");
            catalog.types.Clear();
            foreach (var (id, name, order, group, stack) in DefaultTypes)
            {
                var path = $"{TypesFolder}/CardType_{id}.asset";
                var typeAsset = LoadOrCreate<CardTypeDefinition>(path);
                typeAsset.typeId = id;
                typeAsset.displayName = name;
                typeAsset.sortOrder = order;
                typeAsset.groupId = group;
                typeAsset.defaultStackable = stack;
                EditorUtility.SetDirty(typeAsset);
                catalog.types.Add(typeAsset);
            }
            EditorUtility.SetDirty(catalog);

            CardTypeDefinition T(string id) => catalog.FindById(id);

            var hero = MakeCard("Card_采药人.asset", "renwu.caiyaoren", "采药人",
                "擅长采药的人物卡。", T("renwu"), false,
                ("wood", 2), ("earth", 1));
            hero.equipSlots = new List<string> { "weapon", "armor", "accessory" };

            var herbA = MakeCard("Card_一级药材A.asset", "yaocai.yi_ji_a", "一级药材A",
                "常见草药。", T("caoyao"), true, ("wood", 2));
            herbA.stackable = true;

            var shovel = MakeCard("Card_铁铲.asset", "gongju.tieshan", "铁铲",
                "挖掘用工具，可装备。", T("gongju"), false, ("metal", 1), ("earth", 1));
            shovel.equipSlotId = "weapon";
            shovel.equipModifiers = new List<AttributeValue>
            {
                new AttributeValue { attributeId = "earth", value = 1 }
            };

            var herbB = MakeCard("Card_灵芝.asset", "caoyao.lingzhi", "灵芝",
                "珍稀草药。", T("caoyao"), true, ("wood", 3), ("fire", 1));
            herbB.stackable = true;

            var ore = MakeCard("Card_朱砂.asset", "kuangshi.zhusha", "朱砂",
                "炼丹常用矿石。", T("kuangshi"), true, ("fire", 2), ("metal", 1));
            ore.stackable = true;

            var food = MakeCard("Card_灵谷饭.asset", "shiwu.linggufan", "灵谷饭",
                "吃了能补木气。", T("shiwu"), true, ("wood", 1));
            food.stackable = true;
            food.onEat = new List<AttributeValue> { new AttributeValue { attributeId = "wood", value = 1 } };

            var npc = MakeCard("Card_贾丽拉.asset", "renwu.jialila", "贾丽拉",
                "命运多舛的女子。开局在牌堆。", T("renwu"), false, ("fire", 2), ("water", 1));

            var pill = MakeCard("Card_培元丹.asset", "danyao.peiyuan", "培元丹",
                "炼成的丹药。", T("danyao"), true, ("wood", 2), ("earth", 1));
            pill.stackable = true;
            pill.onEat = new List<AttributeValue>
            {
                new AttributeValue { attributeId = "wood", value = 2 },
                new AttributeValue { attributeId = "earth", value = 1 }
            };

            var failPill = MakeCard("Card_废丹.asset", "danyao.feidan", "废丹",
                "失败产物。", T("danyao"), true);

            var furnace = MakeCard("Card_青铜炉.asset", "danlu.qingtong", "青铜炉",
                "稳火的丹炉。", T("danlu"), false, ("fire", 1), ("metal", 1));

            var recipe = MakeCard("Card_培元丹方.asset", "peifang.peiyuan", "培元丹方",
                "需木火调和。", T("peifang"), false);
            recipe.recipe = new RecipeData
            {
                bands = new List<RecipeAttributeBand>
                {
                    new RecipeAttributeBand { attributeId = "wood", min = 4, max = 9 },
                    new RecipeAttributeBand { attributeId = "fire", min = 6, max = 9 },
                    new RecipeAttributeBand { attributeId = "earth", min = 2, max = 8 }
                },
                requiredHerbTypeIds = new List<string> { "caoyao", "kuangshi" },
                highOutput = pill,
                midOutput = pill,
                failOutput = failPill,
                explodeAttributeId = "fire"
            };

            var book = MakeCard("Card_本草笔记.asset", "shuji.bencao", "本草笔记",
                "记录草药知识。", T("shuji"), false, ("wood", 1));
            var seed = MakeCard("Card_药草种子.asset", "zhongzi.yaocao", "药草种子",
                "可种植。", T("zhongzi"), true, ("wood", 1));
            seed.stackable = true;
            var intel = MakeCard("Card_山道传闻.asset", "qingbao.shandao", "山道传闻",
                "关于山上的情报。", T("qingbao"), false);
            var armor = MakeCard("Card_布衣.asset", "zhuangbei.buyi", "布衣",
                "寻常衣物。", T("zhuangbei"), false, ("earth", 1));
            armor.equipSlotId = "armor";

            // 采药事件 + 分支
            var evt = LoadOrCreate<EventData>(EventsFolder + "/Event_上山采药.asset");
            evt.eventId = "event_shangshan_caiyao";
            evt.title = "上山采药";
            evt.openingText = "山路边有一丛药草。需要合适的人来采，也可以带上工具。";
            evt.previewOutcomeText = "若属性足够，你将采得草药与种子；不足则可能空手而归。";
            evt.requiredAttributes = new ElementRequirement { wood = 2, earth = 1 };
            evt.requirements = new List<AttributeRequirement>
            {
                new AttributeRequirement { attributeId = "wood", required = 2 },
                new AttributeRequirement { attributeId = "earth", required = 1 }
            };
            evt.slots = new List<EventSlotData>
            {
                new EventSlotData
                {
                    slotId = "slot_person",
                    acceptType = T("renwu"),
                    acceptTypes = new List<CardTypeDefinition> { T("renwu") },
                    anchoredPosition = new Vector2(-200f, 0f),
                    required = true
                },
                new EventSlotData
                {
                    slotId = "slot_tool",
                    acceptType = T("gongju"),
                    acceptTypes = new List<CardTypeDefinition> { T("gongju") },
                    anchoredPosition = new Vector2(0f, 0f),
                    required = false
                }
            };
            evt.outcomes = new List<EventOutcome>
            {
                new EventOutcome
                {
                    outcomeId = "high_fire",
                    previewText = "火气旺盛，采得珍稀灵芝。",
                    resultText = "你采到了灵芝！",
                    conditions = new List<SettlementCondition>
                    {
                        new CondAttributeTotal { attributeId = "fire", op = CondAttributeTotal.Op.Greater, value = 10 }
                    },
                    commands = new List<SettlementCommand>
                    {
                        new CmdGrantCard { card = herbB, count = 1 },
                        new CmdMarkEventCompleted { eventId = evt.eventId },
                        new CmdShowResult { text = "你采到了灵芝！" }
                    }
                },
                new EventOutcome
                {
                    outcomeId = "full",
                    previewText = "顺利采得草药与种子。",
                    resultText = "采药成功。",
                    conditions = new List<SettlementCondition>
                    {
                        new CondRequirementTier { tier = RequirementTier.Full }
                    },
                    commands = new List<SettlementCommand>
                    {
                        new CmdGrantCard { card = herbA, count = 2 },
                        new CmdGrantCard { card = seed, count = 1 },
                        new CmdMarkEventCompleted { eventId = evt.eventId },
                        new CmdShowResult { text = "采药成功。" }
                    }
                },
                new EventOutcome
                {
                    outcomeId = "fallback",
                    previewText = "似乎收获有限……",
                    resultText = "空手而归。",
                    commands = new List<SettlementCommand>
                    {
                        new CmdMarkEventCompleted { eventId = evt.eventId },
                        new CmdShowResult { text = "空手而归。" }
                    }
                }
            };
            EditorUtility.SetDirty(evt);

            // 处理尸体 → 开关
            var corpse = LoadOrCreate<EventData>(EventsFolder + "/Event_处理尸体.asset");
            corpse.eventId = "event_chuli_shiti";
            corpse.title = "帮贾丽拉处理尸体";
            corpse.openingText = "贾丽拉需要你帮忙处理一具尸体。";
            corpse.previewOutcomeText = "完成后她会欠你一个人情。";
            corpse.requirements = new List<AttributeRequirement>();
            corpse.slots = new List<EventSlotData>
            {
                new EventSlotData
                {
                    slotId = "slot_person",
                    acceptType = T("renwu"),
                    acceptTypes = new List<CardTypeDefinition> { T("renwu") },
                    anchoredPosition = Vector2.zero,
                    required = true
                }
            };
            corpse.outcomes = new List<EventOutcome>
            {
                new EventOutcome
                {
                    outcomeId = "ok",
                    previewText = "事情办妥。",
                    resultText = "尸体已处理。贾丽拉记住了你。",
                    commands = new List<SettlementCommand>
                    {
                        new CmdSetSwitch { switchKey = "jialila_corpse_done", value = true },
                        new CmdMarkEventCompleted { eventId = corpse.eventId },
                        new CmdShowResult { text = "尸体已处理。贾丽拉记住了你。" }
                    }
                }
            };
            EditorUtility.SetDirty(corpse);

            var ally = LoadOrCreate<EventData>(EventsFolder + "/Event_贾丽拉入队.asset");
            ally.eventId = "event_jialila_join";
            ally.title = "贾丽拉成为盟友";
            ally.openingText = "贾丽拉决定与你同行。";
            ally.previewOutcomeText = "她将加入你的手牌。";
            ally.autoStartOnRound = true;
            ally.repeatable = false;
            ally.appearConditions = new List<SettlementCondition>
            {
                new CondSwitchIs { switchKey = "jialila_corpse_done", expected = true },
                new CondCardAlive { cardId = "renwu.jialila", alive = true }
            };
            ally.slots = new List<EventSlotData>();
            ally.outcomes = new List<EventOutcome>
            {
                new EventOutcome
                {
                    outcomeId = "join",
                    previewText = "贾丽拉入队。",
                    resultText = "贾丽拉加入了你。",
                    commands = new List<SettlementCommand>
                    {
                        new CmdMoveCard
                        {
                            cardId = "renwu.jialila",
                            from = CardLocation.Deck,
                            to = CardLocation.Hand
                        },
                        new CmdMarkEventCompleted { eventId = ally.eventId },
                        new CmdShowResult { text = "贾丽拉加入了你。" }
                    }
                }
            };
            EditorUtility.SetDirty(ally);

            // 炼丹事件
            var brew = LoadOrCreate<EventData>(EventsFolder + "/Event_炼丹.asset");
            brew.eventId = "event_liandan";
            brew.title = "炼丹";
            brew.openingText = "架起丹炉，按丹方投入药材。";
            brew.previewOutcomeText = "火候决定品级。";
            brew.requirements = new List<AttributeRequirement>
            {
                new AttributeRequirement { attributeId = "wood", required = 4 },
                new AttributeRequirement { attributeId = "fire", required = 6 }
            };
            brew.slots = new List<EventSlotData>
            {
                Slot("slot_recipe", T("peifang"), new Vector2(-300, 0), true),
                Slot("slot_person", T("renwu"), new Vector2(-180, 0), true),
                Slot("slot_main", null, new Vector2(-60, 0), false, "herb"),
                Slot("slot_sub", null, new Vector2(60, 0), false, "herb"),
                Slot("slot_catalyst", null, new Vector2(180, 0), false, "herb"),
                Slot("slot_furnace", T("danlu"), new Vector2(300, 0), false)
            };
            brew.outcomes = new List<EventOutcome>
            {
                new EventOutcome
                {
                    outcomeId = "explode",
                    previewText = "火势失控……",
                    resultText = "炸炉！",
                    conditions = new List<SettlementCondition>
                    {
                        new CondRecipeQuality { quality = RecipeQuality.Explode }
                    },
                    commands = new List<SettlementCommand>
                    {
                        new CmdGrantRecipeOutput(),
                        new CmdMarkEventCompleted { eventId = brew.eventId }
                    }
                },
                new EventOutcome
                {
                    outcomeId = "high",
                    previewText = "丹成上品。",
                    resultText = "炼成上品丹药。",
                    conditions = new List<SettlementCondition>
                    {
                        new CondRecipeQuality { quality = RecipeQuality.High }
                    },
                    commands = new List<SettlementCommand>
                    {
                        new CmdGrantRecipeOutput(),
                        new CmdMarkEventCompleted { eventId = brew.eventId }
                    }
                },
                new EventOutcome
                {
                    outcomeId = "mid",
                    previewText = "丹成中品。",
                    resultText = "炼成中品丹药。",
                    conditions = new List<SettlementCondition>
                    {
                        new CondRecipeQuality { quality = RecipeQuality.Mid }
                    },
                    commands = new List<SettlementCommand>
                    {
                        new CmdGrantRecipeOutput(),
                        new CmdMarkEventCompleted { eventId = brew.eventId }
                    }
                },
                new EventOutcome
                {
                    outcomeId = "fail",
                    previewText = "似乎要失败……",
                    resultText = "只炼出废丹。",
                    commands = new List<SettlementCommand>
                    {
                        new CmdGrantRecipeOutput(),
                        new CmdMarkEventCompleted { eventId = brew.eventId }
                    }
                }
            };
            EditorUtility.SetDirty(brew);

            var common = LoadOrCreate<CommonEventData>(CommonFolder + "/Common_贾丽拉入队检查.asset");
            common.commonEventId = "common_jialila_join_check";
            common.title = "贾丽拉入队检查";
            common.trigger = CommonEventTrigger.OnRoundStart;
            common.once = true;
            common.conditions = new List<SettlementCondition>
            {
                new CondSwitchIs { switchKey = "jialila_corpse_done", expected = true },
                new CondCardAlive { cardId = "renwu.jialila", alive = true },
                new CondEventCompleted { eventId = "event_jialila_join", completed = false }
            };
            common.commands = new List<SettlementCommand>
            {
                new CmdOpenEvent { eventData = ally }
            };
            EditorUtility.SetDirty(common);

            var deck = LoadOrCreate<HandDeckConfig>(CardsFolder + "/HandDeckConfig.asset");
            deck.startingCards = new List<CardData>();
            deck.entries = new List<DeckEntry>
            {
                new DeckEntry { card = hero, count = 1, initialLocation = CardLocation.Hand },
                new DeckEntry { card = herbA, count = 3, initialLocation = CardLocation.Hand },
                new DeckEntry { card = shovel, count = 1, initialLocation = CardLocation.Hand },
                new DeckEntry { card = food, count = 2, initialLocation = CardLocation.Hand },
                new DeckEntry { card = ore, count = 2, initialLocation = CardLocation.Hand },
                new DeckEntry { card = recipe, count = 1, initialLocation = CardLocation.Hand },
                new DeckEntry { card = furnace, count = 1, initialLocation = CardLocation.Hand },
                new DeckEntry { card = book, count = 1, initialLocation = CardLocation.Hand },
                new DeckEntry { card = armor, count = 1, initialLocation = CardLocation.Hand },
                new DeckEntry { card = intel, count = 1, initialLocation = CardLocation.Hand },
                new DeckEntry { card = seed, count = 2, initialLocation = CardLocation.Hand },
                new DeckEntry { card = npc, count = 1, initialLocation = CardLocation.Deck }
            };
            EditorUtility.SetDirty(deck);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("已生成属性目录、细分类、起始卡牌、结算分支、公共事件与炼丹事件。");
        }

        static EventSlotData Slot(string id, CardTypeDefinition type, Vector2 pos, bool required, string group = null)
        {
            var s = new EventSlotData
            {
                slotId = id,
                acceptType = type,
                acceptTypes = type != null ? new List<CardTypeDefinition> { type } : new List<CardTypeDefinition>(),
                acceptGroup = group,
                anchoredPosition = pos,
                required = required
            };
            return s;
        }

        static CardData MakeCard(string file, string cardId, string title, string desc,
            CardTypeDefinition type, bool stackable, params (string id, int v)[] attrs)
        {
            var card = LoadOrCreate<CardData>(CardsFolder + "/" + file);
            card.cardId = cardId;
            card.title = title;
            card.description = desc;
            card.cardType = type;
            card.stackable = stackable || (type != null && type.defaultStackable);
            card.maxStack = 99;
            card.attributes = new List<AttributeValue>();
            foreach (var (id, v) in attrs)
                card.attributes.Add(new AttributeValue { attributeId = id, value = v });
            // 清空旧字段
            card.elementMetal = card.elementWood = card.elementWater = card.elementFire = card.elementEarth = 0;
            EditorUtility.SetDirty(card);
            return card;
        }

        [MenuItem("炼丹游戏/迁移旧五行到属性列表")]
        public static void MigrateElements()
        {
            var guids = AssetDatabase.FindAssets("t:CardData");
            int n = 0;
            foreach (var g in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var card = AssetDatabase.LoadAssetAtPath<CardData>(path);
                if (card == null) continue;
                bool changed = false;
                void Mig(string id, int v)
                {
                    if (v == 0) return;
                    if (card.GetAttribute(id) == 0)
                    {
                        card.SetAttribute(id, v);
                        changed = true;
                    }
                }
                Mig("metal", card.elementMetal);
                Mig("wood", card.elementWood);
                Mig("water", card.elementWater);
                Mig("fire", card.elementFire);
                Mig("earth", card.elementEarth);
                if (changed)
                {
                    EditorUtility.SetDirty(card);
                    n++;
                }
            }

            var eGuids = AssetDatabase.FindAssets("t:EventData");
            foreach (var g in eGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(g);
                var ev = AssetDatabase.LoadAssetAtPath<EventData>(path);
                if (ev == null) continue;
                if ((ev.requirements == null || ev.requirements.Count == 0) && ev.requiredAttributes != null)
                {
                    ev.requirements = ev.requiredAttributes.ToAttributeList();
                    EditorUtility.SetDirty(ev);
                    n++;
                }
            }

            AssetDatabase.SaveAssets();
            Debug.Log($"迁移完成，更新 {n} 个资产。");
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
