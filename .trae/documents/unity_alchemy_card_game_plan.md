# 炼丹卡牌叙事游戏 - 完整设计方案

## 一、概要

玩家在主界面（地毯式地图）上点击随机事件节点进入事件界面，通过选择按钮和放置卡牌触发金木水火土属性判定，获得药材、丹方、种子等资源，再进入炼丹系统完成资源获取→加工处理→核心炼制的完整循环。

**技术栈：** Unity 6 (6000.5.4f1)，2D URP，New Input System，UGUI，DOTween（需导入）
**项目位置：** `c:\Users\YXY\JOJO\XHHYHDYX`
**现有资产：** Frame 1-3.png（界面参考图）、Rectangle4/8.png（UI占位框）、CameraPanZoom.cs（摄像机控制）

---

## 二、系统设计分析

### 2.1 四模块架构

根据设计图，整个游戏分为四个核心模块，构成完整的炼丹循环：

```
┌─────────────────────────────────────────────────────┐
│                    辅助系统                           │
│  药物食物→临时提升技能→人物技能点→缩短动作时间        │
│  书籍研读残页→发现遗迹药材                            │
└──────────┬──────────────────────┬───────────────────┘
           │                      │
┌──────────▼──────────┐  ┌───────▼──────────────────┐
│    资源获取           │  │    加工处理               │
│  采集点探索           │  │  药田(种植作物)           │
│    ↓                 │  │    → 收割新药材           │
│  属性判定             │  │    → 药碾台(修改药材属性)  │
│  ├成功→材料/种子/丹方 │  │  药材/粉末→药碾台         │
│  └失败→角色血量减少   │  └──────────┬───────────────┘
└──────────────────────┘             │
                           ┌─────────▼─────────────────┐
                           │      核心炼制               │
                           │  炼丹需求：主材+辅材+属性    │
                           │    → 炼丹判定              │
                           │    ├成功→获得丹药           │
                           │    └失败→炼丹失败           │
                           └───────────────────────────┘
```

### 2.2 核心系统拆解

**灵觉系统：** 学习的起点，不可重复，必须有触发条件，玩家可以选择要做什么。灵觉是进入炼丹系统的入口门槛。

**属性系统：** 金、木、水、火、土五种属性，随机赋予。属性点决定能否满足效力需求，决定能否产出高级物品。属性是贯穿全系统的核心判定维度。

**人物触点系统：** 所有操作遵循"1人+1物"成本模型。种植、砍伐、制作、学习等操作通过与触发物/动物/植物接触获得人物触点。每个触点消耗一个角色和一个资源。

**丹药系统：** 9种丹药按药用分类，含1种炼丹类型、3种内丹类型、动物内丹、修行丹（分多级）、植物丹（分多级）。数值越大分数越多。

**丹方系统：** 丹方决定丹药类型，可购买。高级丹方需要高级材料（≥2维属性、高阶属性）。

**丹炉系统：** 丹炉可升级，分多个等级。等级影响炼丹成功率和产出品质。

**丹谱系统：** 记录已尝试的配方和功能，可查阅。是知识积累和配方解锁的载体。

**事件系统：** 分为灵觉事件（采集、服丹触发，限1次）、丹成事件（丹药炼制完成触发，限1次）、条件事件（满足特定条件触发）、定时事件（按天数周期触发）。

### 2.3 完整流程

```
开始 → 采集点探索 → 属性判定
  ├─ 成功 → 获得材料/种子/随机物品
  │   ├─ 首次探索 → 获得丹方
  │   ├─ 获得药材 → 药材/粉末 → 药碾台(修改属性)
  │   └─ 获得种子 → 药田(种植) → 收割新药材 → 药碾台(修改属性)
  │
  └─ 失败 → 角色血量减少

药碾台 → 炼丹需求(主材+辅材+属性) → 炼丹判定
  ├─ 成功 → 获得丹药 → 服丹(提升属性)
  └─ 失败 → 炼丹失败 → 丹渣(可堆肥)

辅助系统：
  药物食物 → 临时提升技能 → 人物技能点 → 缩短动作时间 → 影响炼丹过程
  书籍研读残页 → 发现遗迹药材
```

---

## 三、架构设计

### 3.1 单场景 + 多面板

所有界面（地图、事件、炼丹、加工、药田、冒险、丹谱）都是同一场景中 Canvas 下的不同 Panel，通过状态机控制显隐。卡牌可跨界面拖拽，状态保持连贯，切换无加载延迟。

```
GameController (全局状态机, 单例, DontDestroyOnLoad)
  |
  +-- GameStateMachine (状态机核心)
  |     +-- BootState        (初始化)
  |     +-- MapState         (主地图)
  |     +-- EventState       (事件/仪式)
  |     +-- AlchemyState     (核心炼制)
  |     +-- ProcessState     (加工处理: 研磨/蒸馏/升炼)
  |     +-- FieldState       (药田: 种植/堆肥)
  |     +-- AdventureState   (采药冒险)
  |     +-- BlueprintState   (图纸库: 查看/制造)
  |     +-- DanPuState       (丹谱: 查阅/解锁)
  |     +-- ResultState      (结算面板)
  |     +-- GameOverState    (结局)
  |
  +-- 各子系统 Manager (通过事件总线通信)
        +-- CardManager         (卡牌实例池/手牌)
        +-- ResourceManager     (5种资源)
        +-- CharacterManager    (角色/技能/血量/创伤)
        +-- TimeManager         (天数/"下一天"结算管线)
        +-- MapManager          (地图节点/页面)
        +-- EventManager        (事件触发/结算)
        +-- AlchemyManager      (炼丹判定/属性对比)
        +-- ProcessController   (多阶段加工: 研磨/蒸馏/升炼)
        +-- ToolManager         (工具卡牌/品质升级链)
        +-- BlueprintManager    (图纸库/学习/制造)
        +-- AdventureManager    (采药冒险/属性判定)
        +-- FieldController     (药田种植+堆肥)
        +-- DanPuManager        (丹谱记录/配方解锁)
        +-- LingJueManager      (灵觉触发/学习入口)
        +-- InventoryManager    (药田/药架/书架/容器/图纸库)
```

### 3.2 ScriptableObject 事件总线

模块间通过 ScriptableObject 事件通道通信，避免硬引用耦合。发布者调用 `Raise()`，订阅者在 Inspector 中绑定监听。

核心事件通道：
- `OnDayAdvanced`（下一天结算完成）
- `OnCardDragStarted` / `OnCardDropped`（卡牌拖拽）
- `OnRiteResolved`（仪式结算完成）
- `OnResourceChanged`（资源变化）
- `OnStateChanged`（游戏状态切换）
- `OnCardVanished`（倒计时归零触发）
- `OnProcessStarted` / `OnProcessCompleted`（加工开始/完成）
- `OnAdventureStarted` / `OnAdventureResolved`（冒险出发/结算）
- `OnBlueprintLearned` / `OnBlueprintCrafted`（图纸学习/制造完成）
- `OnToolUpgraded`（工具品质升级）
- `OnGardenPlanted` / `OnGardenHarvested` / `OnCompostComplete`（药田种植/收获/堆肥）
- `OnLingJueTriggered`（灵觉触发）
- `OnDanPuUpdated`（丹谱记录更新）
- `OnPillRefined`（丹药炼制成功）

### 3.3 "下一天"结算管线

```
玩家点击"下一天"
  → Step 1:  所有卡牌倒计时 -1
  → Step 2:  检查倒计时归零的卡牌，触发对应事件
  → Step 3:  结算当前挂起的仪式——属性判定 → 产出结果
  → Step 4:  执行仪式附加效果
  → Step 5:  更新角色临时加成（减少持续回合）
  → Step 6:  药田生长更新（生长倒计时-1，堆肥分解产出属性肥料）
  → Step 7:  加工流水线结算（Duration-1，归零的加工产出新材料卡）
  → Step 8:  采药冒险结算（冒险倒计时-1，归零的冒险执行属性判定→奖励/创伤）
  → Step 9:  触发定时/随机事件
  → Step 10: 天数+1，重置时间，回到地图界面
```

### 3.4 目录结构

```
Assets/
├── Art/                          (已有)
├── Scenes/SampleScene.unity      (已有)
├── Settings/                     (已有)
├── Scripts/
│   ├── Runtime/
│   │   ├── Core/                 (GameController, GameStateMachine, GameStateBase, ServiceLocator, GameBootstrap)
│   │   ├── Events/               (GameEvent, GameEventT, GameEventListener)
│   │   ├── Data/                 (Enums, CardData, EventData, EventChoice, RecipeData, PillData, MapNodeData, CharacterData, EffectData, WorkbenchSlot, ToolData, BlueprintData, ProcessStageData, GardenPlotData, AdventureData, DanPuData, LingJueData, TraumaData)
│   │   ├── Cards/                (CardInstance, CardManager, CardController, CardSlotController, CardAttributeController, CardTagController, HandCardsController)
│   │   ├── Interaction/          (SimpleDragable, DropDelegater, PointerClickDelegater)
│   │   ├── Events/               (EventManager, RiteController, RitePanelController, RiteResultPanelController, DiceController)
│   │   ├── Alchemy/              (AlchemyManager, AlchemyController, ProcessController, ToolManager, BlueprintManager, AdventureManager, FieldController, PropertyComparator, DanPuManager, LingJueManager)
│   │   ├── Map/                  (MapManager, MapController, MapNodeController)
│   │   ├── Game/                 (ResourceManager, CharacterManager, TimeManager, InventoryManager)
│   │   └── UI/                   (UIManager, PanelBase, MapPanelController, EventPanelController, AlchemyPanelController, ProcessPanelController, FieldPanelController, AdventurePanelController, BlueprintPanelController, DanPuPanelController, ResultPanelController, CardBarUI, ResourceBarUI, PageSwitcherUI, WorkbenchCardUI)
│   └── Editor/                   (CardDataEditor, EventDataEditor, RecipeDataEditor, BlueprintDataEditor)
├── Prefabs/
│   ├── UI/                       (MapPanel, EventPanel, AlchemyPanel, ProcessPanel, FieldPanel, AdventurePanel, BlueprintPanel, DanPuPanel, ResultPanel, CardBar, ResourceBar)
│   └── Cards/                    (Card_Character, Card_Herb, Card_Pill, Card_Recipe, Card_Item, Card_Tool_Grinder, Card_Tool_Distiller, Card_Tool_Furnace, Card_Seed)
├── Data/                         (ScriptableObject 实例资产)
│   ├── Events/  Recipes/  Cards/  MapNodes/  Characters/  GameEvents/  Blueprints/  Adventures/  Tools/  DanPu/
└── Plugins/DOTween/              (需导入)
```

---

## 四、数据模型设计

### 4.1 枚举定义 `Enums.cs`

```csharp
// 卡牌类型
public enum CardType { Character, Herb, Pill, Recipe, Tool, Book, Food, Seed }

// 五行属性（核心判定维度）
public enum ElementType { Metal, Wood, Water, Fire, Earth }

// 技能类型
public enum SkillType { Gathering, Farming, Crafting, Alchemy, Combat, Interaction }

// 游戏状态
public enum GameStateType { Boot, MapView, EventView, AlchemyView, ProcessView, FieldView, AdventureView, BlueprintView, DanPuView, ResultView, GameOver }

// 事件触发类型
public enum EventTriggerType { Regular, LingJue, DanCheng, Conditional, Scheduled }

// 效果类型
public enum EffectType { AddResource, AddCard, RemoveCard, DamageCharacter, HealCharacter, BoostSkill, AdvanceTime, TriggerEvent, DiscoverHerb, ModifyAttribute, AddTrauma, BoostElement, CompostToProperty, LearnBlueprint, UpdateDanPu }

// 地点类型
public enum LocationType { Map, HerbField, Workbench, PillShelf, Bookshelf, Storage, AdventureBoard }

// 判定结果
public enum CheckResult { Success, Failure, CriticalSuccess, CriticalFailure }

// 工具类型（卡牌即工作台：药碾台/蒸馏器/丹炉）
public enum ToolType { Grinder, Distiller, Furnace, Universal }

// 工具品质
public enum ToolTier { Level1, Level2, Level3, Level4, Level5 }

// 加工阶段
public enum ProcessStage { Grinding, Distilling, Refining, Brewing }

// 图纸类型
public enum BlueprintType { Building, Tool, Recipe, Structure }

// 药田状态
public enum GardenState { Empty, Tilled, Planted, Growing, Ready }

// 冒险状态
public enum AdventureState { Available, InProgress, Completed, Failed }

// 丹药效果类型
public enum PillEffectType { BoostElement, HealTrauma, CurePoison, EnhanceSkill, TempBoost, FatalRisk }

// 灵觉状态
public enum LingJueState { Locked, Available, Triggered, Completed }

// 丹谱条目类型
public enum DanPuEntryType { RecipeTried, RecipeDiscovered, HerbDiscovered, PillRefined, FunctionUnlocked }
```

### 4.2 卡牌数据 `CardData.cs`

```csharp
[CreateAssetMenu(fileName = "NewCard", menuName = "炼丹/卡牌数据")]
public class CardData : ScriptableObject
{
    public string cardId;
    public string title;
    [TextArea(2, 4)] public string description;
    public CardType cardType;
    public int level;                           // 等级（1级、2级、3级...）
    public string variant;                      // 变体编号（A、B、C、D...）
    public Sprite icon, portrait, frame;

    public List<ElementValue> elements;         // 金木水火土属性值
    public List<string> tags;                   // 标签（条件判定用）
    public int vanishCountdown;                 // 倒计时回合数
    public EventData vanishEvent;               // 倒计时归零触发的事件
    public List<EffectData> postRiteEffects;    // 仪式附加效果
    public int timeCost, personCost;            // "1人+1物"成本模型
    public RecipeData recipe;                   // 丹方卡专用
    public CharacterData character;             // 角色卡专用
    public PillData pill;                       // 丹药卡专用

    // 卡牌即工作台模式
    public bool isWorkbench;                    // 是否为工作台卡牌（工具/丹炉等）
    public ToolData toolData;                   // 工具属性
    public List<WorkbenchSlot> workbenchSlots;  // 工作台槽位
    public int strongestPillar;                 // 最强属性柱数值
    public ElementType strongestElement;        // 最强属性类型
    public bool isDecomposable;                 // 是否可堆肥
    public List<ElementValue> compostProperties;// 堆肥后产出的属性值
}

[Serializable] public class ElementValue { public ElementType type; public int value; }
```

### 4.3 工作台槽位 `WorkbenchSlot.cs`

```csharp
[Serializable]
public class WorkbenchSlot
{
    public string slotId, slotLabel;
    public CardType acceptType;
    public List<string> acceptTags;
    public bool isMagneticSlot;               // 磁性槽位（自动吸附匹配卡牌）
    public string magneticSlotRequirement;
    public int maxCards;
    public Vector2 slotPosition;
    public bool isRequired;
}
```

### 4.4 工具数据 `ToolData.cs`

```csharp
[Serializable]
public class ToolData
{
    public ToolType toolType;                 // Grinder/Distiller/Furnace/Universal
    public ToolTier tier;                     // 品质等级
    public string toolName;                   // 如"药碾台1级"
    public Sprite toolIcon;

    public float potencyLossRate;             // 研磨后药性损失比例
    public int extraPortionChance;            // 额外产出概率
    public List<ProcessStage> supportedStages;
    public int processingPower;               // 加工能力
    public int slotCount;                     // 工作台槽位数

    // 升级链
    public ToolTier? upgradeFrom;
    public BlueprintData upgradeBlueprint;
    public List<MaterialRequirement> upgradeCosts;
}
```

### 4.5 事件数据 `EventData.cs` + `EventChoice.cs`

```csharp
[CreateAssetMenu(fileName = "NewEvent", menuName = "炼丹/事件数据")]
public class EventData : ScriptableObject
{
    public string eventId;
    public string title;
    [TextArea(3, 6)] public string description;
    public Sprite background;
    public int priority;
    public EventTriggerType triggerType;      // Regular/LingJue/DanCheng/Conditional/Scheduled
    public List<EventChoice> choices;
    public List<CardSlotDefinition> slots;
    public List<ConditionData> triggerConditions;
}

[Serializable]
public class EventChoice
{
    public string choiceId, label;
    public Sprite buttonIcon;                 // 骷髅/交叉剑/勾选
    public ChoiceButtonType buttonType;
    public SkillType checkSkill;
    public int checkDifficulty;
    public bool useDice;
    public List<CardRequirement> requiredCards;
    [TextArea(2, 4)] public string successText, failureText;
    public List<EffectData> successEffects, failureEffects;
}

[Serializable]
public class CardSlotDefinition
{
    public string slotId, slotLabel;
    public CardType acceptType;
    public List<string> acceptTags;
    public bool isRequired;
    public Vector2 slotPosition;
}
```

### 4.6 丹方数据 `RecipeData.cs`

```csharp
[CreateAssetMenu(fileName = "NewRecipe", menuName = "炼丹/丹方数据")]
public class RecipeData : ScriptableObject
{
    public string recipeId, recipeName;       // 如"丹方-回春丹"
    public List<MaterialRequirement> mainMaterials;
    public List<MaterialRequirement> auxiliaryMaterials;
    public List<ElementValue> requiredElements;           // 属性阈值（丹炉需至少达到X金Y木）
    public ElementType? requiredStrongestPillar;          // 起始材料需满足的最强属性柱类型
    public int strongestPillarThreshold;                  // 最强属性柱阈值
    public int requiredAlchemyLevel, fuelCost, timeCost, baseSuccessRate;
    public bool requiresProcessedMaterials;               // 是否需要加工后材料
    public PillData resultPill, failureResult;            // 成功产物/失败产物(丹渣)
    public bool isHighGrade;                              // 是否高级丹方（需≥2维属性材料）
    public BlueprintData sourceBlueprint;                 // 来源图纸
}

[Serializable]
public class MaterialRequirement
{
    public CardData requiredCard;
    public int quantity;
    public int minLevel;                      // 最低等级要求
    public List<string> acceptTags;
}
```

### 4.7 丹药数据 `PillData.cs`

```csharp
[CreateAssetMenu(fileName = "NewPill", menuName = "炼丹/丹药数据")]
public class PillData : ScriptableObject
{
    public string pillId, pillName;           // 如"1级丹药回春丹"
    public Sprite icon;
    public int grade;                         // 品阶
    public int level;                         // 等级
    public List<EffectData> effects;
    public List<ElementValue> elements;
    public int value;

    // 丹药效果
    public PillEffectType effectType;
    public ElementType? boostedElement;       // 提升的属性类型
    public int boostAmount;                   // 提升数值
    public int effectDuration;                // 效果持续天数
    public bool canCureTrauma;
    public int traumaHealAmount;
    public bool isOneAtATime;                 // 同时只能服用一种
    public int pillCategory;                  // 0=药用丹 1=炼丹 2=内丹 3=动物内丹 4=修行丹 5=植物丹
}
```

### 4.8 角色数据 `CharacterData.cs`

```csharp
[CreateAssetMenu(fileName = "NewCharacter", menuName = "炼丹/角色数据")]
public class CharacterData : ScriptableObject
{
    public string characterId, characterName;
    public Sprite portrait;
    public int maxHp, currentHp;
    public List<SkillLevel> skills;
    public List<TempSkillBoost> tempBoosts;
    public List<string> tags;

    // 五行属性 + 创伤系统
    public List<ElementValue> daoProperties;  // 金木水火土属性值
    public List<TraumaData> traumas;          // 创伤列表
    public int maxTraumas;                    // 最大创伤数
    public PillData activePill;               // 当前服用的丹药
    public int activePillRemainingDays;       // 丹药效果剩余天数
}

[Serializable]
public class SkillLevel { public SkillType type; public int level; }

[Serializable]
public class TempSkillBoost { public SkillType skill; public int amount; public int remainingDays; }

[Serializable]
public class TraumaData
{
    public ElementType boostedElement;
    public int boostAmount;
    public string traumaName;
    public string traumaDescription;
}
```

### 4.9 地图节点 `MapNodeData.cs`

```csharp
[CreateAssetMenu(fileName = "NewMapNode", menuName = "炼丹/地图节点")]
public class MapNodeData : ScriptableObject
{
    public string nodeId, nodeName;           // 如"采药地图1级-A"
    public Sprite nodeIcon;
    public Vector2 position;
    public int pageIndex;                     // 所属页面 I/II/III/IV
    public int mapLevel;                      // 地图等级
    public string areaCode;                   // 区域编号 A/B/C/D
    public EventData linkedEvent;
    public LocationType locationType;
    public List<string> connectedNodeIds;
    public bool isLocked = true, isExplored = false;
    public bool isFirstExplore = true;        // 首次探索获丹方
}
```

### 4.10 图纸数据 `BlueprintData.cs`

```csharp
[CreateAssetMenu(fileName = "NewBlueprint", menuName = "炼丹/图纸数据")]
public class BlueprintData : ScriptableObject
{
    public string blueprintId, blueprintName; // 如"图纸-药碾台2级"
    public BlueprintType blueprintType;
    public Sprite blueprintIcon;
    [TextArea(2, 4)] public string description;

    public List<MaterialRequirement> requiredMaterials;
    public CardData prerequisiteBuilding;
    public List<ResourceCost> resourceCosts;
    public int timeCost;
    public CardData resultCard;
    public bool isLearned;
}

[Serializable]
public class ResourceCost { public ResourceType type; public int amount; }
```

### 4.11 加工阶段数据 `ProcessStageData.cs`

```csharp
[Serializable]
public class ProcessStageData
{
    public ProcessStage stage;                // Grinding/Distilling/Refining/Brewing
    public string stageName;
    public int duration;                      // 加工时长（天数）
    public ToolType requiredTool;

    public List<ElementValue> inputRequirements;
    public List<ElementValue> outputModifiers;    // 输出属性修改

    public List<ElementValue> workerPropertyRequirements;
    public int successThreshold;

    public List<MaterialRequirement> inputMaterials;
    public CardData outputCard;
    public int outputQuantity;
    public bool isOptional;
}
```

### 4.12 药田数据 `GardenPlotData.cs`

```csharp
[Serializable]
public class GardenPlotData
{
    public string plotId;
    public GardenState state;
    public CardData plantedSeed;              // 种子卡（如"1级种子A"）
    public CardData compostItem;              // 堆肥物
    public int growthDuration;
    public int remainingDays;
    public CardData harvestResult;            // 收获产物（如"1级药材A"）
    public List<ElementValue> compostResult;  // 堆肥产出的属性值
    public CharacterData assignedGardener;
    public int gardenerSpeedBonus;
}
```

### 4.13 冒险数据 `AdventureData.cs`

```csharp
[CreateAssetMenu(fileName = "NewAdventure", menuName = "炼丹/冒险数据")]
public class AdventureData : ScriptableObject
{
    public string adventureId, adventureName; // 如"采药地图1级-A"
    public Sprite locationIcon;
    [TextArea(3, 6)] public string description;
    public AdventureState state;
    public int mapLevel;                      // 地图等级
    public string areaCode;                   // 区域编号

    public int fundCost, foodCost;
    public int duration, remainingDays;

    public List<WorkbenchSlot> adventureSlots; // 冒险者槽/药剂槽
    public List<ElementValue> preferredElements; // 偏好的五行属性
    public string preferredElementDescription;
    public int successThreshold;

    public List<AdventureOutcome> possibleOutcomes;
}

[Serializable]
public class AdventureOutcome
{
    public CheckResult resultType;
    public int weight;
    [TextArea(2, 4)] public string outcomeText;
    public List<EffectData> effects;
    public List<CardData> rewardCards;
}
```

### 4.14 丹谱数据 `DanPuData.cs`

```csharp
[CreateAssetMenu(fileName = "NewDanPu", menuName = "炼丹/丹谱数据")]
public class DanPuData : ScriptableObject
{
    public string danPuId;
    public List<DanPuEntry> entries;          // 丹谱条目列表

    public void AddEntry(DanPuEntryType type, string content, RecipeData recipe = null)
    {
        // 记录已尝试的配方、发现的药材、炼成的丹药、解锁的功能
    }

    public bool HasRecipe(RecipeData recipe) { /* 检查丹方是否已记录 */ }
    public List<RecipeData> GetDiscoveredRecipes() { /* 获取已发现丹方 */ }
}

[Serializable]
public class DanPuEntry
{
    public DanPuEntryType entryType;
    public string entryContent;
    public RecipeData relatedRecipe;
    public int dayRecorded;
}
```

### 4.15 灵觉数据 `LingJueData.cs`

```csharp
[CreateAssetMenu(fileName = "NewLingJue", menuName = "炼丹/灵觉数据")]
public class LingJueData : ScriptableObject
{
    public string lingJueId, lingJueName;
    [TextArea(3, 6)] public string description;
    public LingJueState state;                // Locked/Available/Triggered/Completed
    public List<ConditionData> triggerConditions;
    public List<EffectData> completeEffects;  // 完成灵觉后的效果
    public List<RecipeData> unlockedRecipes;  // 解锁的丹方
    public List<BlueprintData> unlockedBlueprints; // 解锁的图纸
    public EventData triggeredEvent;          // 触发的事件
}
```

### 4.16 效果与条件 `EffectData.cs`

```csharp
[Serializable]
public class EffectData
{
    public EffectType effectType;
    public ResourceType resourceType; public int amount;
    public CardData cardToGive; public int cardCount;
    public int damageAmount, healAmount;
    public SkillType skillToBoost; public int skillBoostAmount, skillBoostDuration;
    public EventData eventToTrigger;
    public ElementType elementToModify; public int elementModifyAmount;
    public List<ConditionData> conditions;
}

[Serializable]
public class ConditionData
{
    public ConditionType conditionType;
    public ResourceType resourceType; public int resourceThreshold;
    public string requiredTag;
    public SkillType requiredSkill; public int skillThreshold;
}
```

---

## 五、核心系统设计

### 5.1 卡牌系统

#### 5.1.1 运行时卡牌实例 `CardInstance.cs`
管理可变状态：当前五行属性值（可被药碾台修改）、倒计时、当前所在槽位、工作台内嵌入的卡牌列表。

#### 5.1.2 卡牌管理器 `CardManager.cs`
- `CreateCard(CardData)` → 创建实例并加入手牌
- `RemoveCard(CardInstance)` → 从手牌移除
- `FindCards(Predicate)` → 查找满足条件的卡牌
- `TickAllCountdowns()` → 倒计时减少
- `GetVanishedCards()` → 获取倒计时归零的卡牌

#### 5.1.3 卡牌控制器 `CardController.cs`
- `Bind(CardInstance)` → 绑定数据
- `RefreshVisual()` → 刷新显示
- `ApplyCardTypeLayout(CardType)` → 按类型切换布局
- 工作台卡牌额外渲染内嵌槽位（材料卡嵌入工具卡内部）

#### 5.1.4 卡牌槽位控制器 `CardSlotController.cs`
- `CanAccept(CardInstance)` → 检查是否接受
- `PlaceCard(CardController)` → 放入卡牌（DOTween吸附动画）
- `OnDrop(PointerEventData)` → IDropHandler 实现
- 磁性槽位自动吸附匹配卡牌

#### 5.1.5 手牌栏 `HandCardsController.cs`
- 底部卡牌栏排列、罗马数字翻页（I/II/III/IV）
- `RefreshHand()` / `SwitchPage(int)`

### 5.2 交互系统

- `SimpleDragable.cs`：IBeginDragHandler/IDragHandler/IEndDragHandler，拖拽跟随鼠标，松手回弹
- `DropDelegater.cs`：IDropHandler，转发Drop事件，拖拽悬停高亮
- `PointerClickDelegater.cs`：IPointerClickHandler，地图节点/按钮点击

**重要：** 项目仅启用 New Input System，EventSystem 必须使用 `InputSystemUIInputModule`。

### 5.3 事件/仪式系统

#### 5.3.1 事件管理器 `EventManager.cs`
- `TriggerEvent(EventData)` → 触发事件
- `SelectChoice(EventChoice)` → 玩家选择
- `ResolveCheck(EventChoice, placedCards)` → 核心判定逻辑：
  1. 计算玩家相关技能总值（角色技能 + 临时加成 + 放入卡牌的属性加成）
  2. 如果 useDice，掷骰子
  3. 对比技能总值+骰子 vs checkDifficulty
  4. 返回成功/失败/大成功/大失败

#### 5.3.2 骰子控制器 `DiceController.cs`
- `Roll(int sides)` / `RollCheck(playerScore, difficulty)` / `PlayDiceAnimation(result, onComplete)`

#### 5.3.3 仪式面板 `RitePanelController.cs`
管理事件UI：标题栏、文本面板、选择按钮（骷髅/交叉剑/勾选）、卡牌槽位、资源栏。

### 5.4 炼丹系统

#### 5.4.1 核心循环

```
资源获取层
  ├── 采集探索（地图节点→事件→属性判定→获得药材/丹方/种子）
  ├── 药田种植（种子→生长→收获药材）
  └── 采药冒险（冒险者+资金+食物+药剂→属性判定→奖励）

加工处理层（卡牌即工作台）
  ├── 研磨（药碾台卡：药材→碎药材，损失部分药性但解锁新丹方）
  ├── 蒸馏（蒸馏器卡：液态材料→精华，提纯属性）
  └── 升炼（丹炉卡：材料→属性精华，改变五行配比）

核心炼制层（丹炉卡）
  ├── 选丹方 → 放入加工后材料 → 放入炼丹角色 → 属性对比判定
  ├── 成功：产出丹药 → 记入丹谱
  └── 失败：产出丹渣（可堆肥回药田）
```

#### 5.4.2 属性对比器 `PropertyComparator.cs`

核心判定逻辑，逐属性计算工人属性值 vs 需求值：

```csharp
public class PropertyComparator
{
    // 返回 (是否有优势, 总优势值)
    public (bool hasAdvantage, int totalAdvantage) Compare(
        List<ElementValue> workerProperties,
        List<ElementValue> requiredElements)
    {
        int totalAdvantage = 0;
        foreach (ElementType elem in Enum.GetValues(typeof(ElementType)))
        {
            int workerValue = GetPropertyValue(workerProperties, elem);
            int requiredValue = GetPropertyValue(requiredElements, elem);
            // 逐属性取差值，不足则为负
            totalAdvantage += (workerValue - requiredValue);
        }
        bool hasAdvantage = totalAdvantage >= 0;
        return (hasAdvantage, totalAdvantage);
    }
}
```

#### 5.4.3 炼丹管理器 `AlchemyManager.cs`
- `SelectRecipe(RecipeData)` → 选择丹方，显示材料需求和属性阈值
- `PlaceMaterial(CardInstance, WorkbenchSlot)` → 放入材料到丹炉工作台槽位
- `PlaceWorker(CardInstance)` → 放入炼丹角色
- `CanBrew()` → 检查丹方需求（主材+辅材+属性阈值+技能+燃料+时间）
- `Brew()` → 核心炼制判定：
  1. 遍历5种五行属性，计算工人属性总值 vs 丹方需求值
  2. 逐属性取差值，求和得总优势值
  3. 总优势值 ≥ 0 → 有优势
  4. 成功率 = baseSuccessRate + 总优势值加成 + 工具品质加成
  5. 掷骰子判定
  6. 成功：消耗材料/燃料/时间，产出丹药，记入丹谱
  7. 失败：产出丹渣

#### 5.4.4 多阶段加工控制器 `ProcessController.cs`

```csharp
public class ProcessController
{
    // 研磨：药材 → 碎药材
    public void GrindHerb(CardInstance herb, CardInstance grinderTool)
    {
        // 1. 检查药碾台工作台槽位
        // 2. 药材放入药碾台槽位
        // 3. 根据药碾台等级计算：
        //    - 药性损失 = herb.elements * (1 - grinder.potencyLossRate)
        //    - 额外产出概率 = grinder.extraPortionChance
        // 4. 设置Duration，加入"下一天"结算队列
        // 5. 产出碎药材卡（带Crushed标签，解锁新丹方）
    }

    // 蒸馏：液态材料 → 精华
    public void Distill(CardInstance liquid, CardInstance distillerTool) { ... }

    // 升炼：材料 → 属性精华
    public void Refine(CardInstance material, CardInstance furnaceTool) { ... }

    // 加工完成回调
    public void OnProcessComplete(ProcessStageData stage, CardInstance input) { ... }
}
```

加工有 Duration（天数），放入材料后需等待"下一天"结算才完成。多个加工可并行。

#### 5.4.5 工具管理器 `ToolManager.cs`
- 管理所有工具卡牌实例（药碾台/蒸馏器/丹炉）
- `GetToolByType(ToolType, ToolTier)` → 按类型和品质查找
- `UpgradeTool(CardInstance tool, BlueprintData blueprint)` → 工具升级
- 工具品质影响：药性损失率、额外产出概率、加工能力、槽位数

**工具升级链：**
```
药碾台: 药碾台1级 → 药碾台2级 → 药碾台3级
蒸馏器: 蒸馏器1级 → 蒸馏器2级 → 蒸馏器3级
丹炉:   丹炉1级 → 丹炉2级 → 丹炉3级 → 丹炉4级 → 丹炉5级
```

#### 5.4.6 药田控制器 `FieldController.cs`

```csharp
public class FieldController
{
    // 种植模式：种子 → 生长 → 收获
    public void Plant(CardInstance seed, int plotIndex, CardInstance gardener)
    {
        // 1. 检查药田地块状态
        // 2. 放入种子卡（如"1级种子A"）
        // 3. 可选：放入农夫卡加速生长
        // 4. 设置growthDuration，加入结算队列
    }

    // 堆肥模式：可分解物 → 属性肥料
    public void Compost(CardInstance decomposable, int plotIndex)
    {
        // 1. 检查isDecomposable标记
        // 2. 放入药田地块
        // 3. 根据compostProperties产出属性肥料
        // 4. 属性可加到该地块后续种植的药材上
    }

    public void Harvest(int plotIndex) → 收获药材卡（如"1级药材A"）
    public void OnDayAdvanced() → 生长倒计时-1
}
```

#### 5.4.7 图纸管理器 `BlueprintManager.cs`
- `LearnBlueprint(BlueprintData)` → 学习图纸（事件奖励/书籍残页发现）
- `GetLearnedBlueprints(BlueprintType)` → 获取某类型已学图纸
- `CanCraft(BlueprintData)` → 检查材料
- `Craft(BlueprintData)` → 消耗材料，产出建筑/工具/丹方卡

#### 5.4.8 冒险管理器 `AdventureManager.cs`
- `StartAdventure(AdventureData, CardInstance adventurer, CardInstance potion)` → 开始冒险
- `ResolveAdventure(AdventureData)` → 冒险结算：
  1. 计算冒险者五行属性 vs 冒险偏好属性
  2. 使用 PropertyComparator 计算总优势值
  3. 按权重选择结果
  4. 执行效果（奖励卡牌/资源/创伤）
- `OnDayAdvanced()` → 冒险倒计时-1

#### 5.4.9 丹谱管理器 `DanPuManager.cs`
- `RecordRecipe(RecipeData, CheckResult)` → 记录丹方尝试结果
- `RecordHerb(CardData)` → 记录新发现的药材
- `RecordPill(PillData)` → 记录炼成的丹药
- `GetDiscoveredRecipes()` → 获取已发现丹方列表
- `IsFunctionUnlocked(string)` → 检查功能是否解锁

#### 5.4.10 灵觉管理器 `LingJueManager.cs`
- `CheckTriggers()` → 检查灵觉触发条件
- `TriggerLingJue(LingJueData)` → 触发灵觉事件
- `CompleteLingJue(LingJueData)` → 完成灵觉，解锁丹方/图纸/功能
- 灵觉不可重复，是进入炼丹系统的学习入口

### 5.5 地图系统

- `MapManager.cs`：管理地图节点、路径、页面切换、节点解锁
- `OnNodeClicked(MapNodeData)` → 锁定则提示解锁条件，未锁定则触发关联事件
- `SwitchPage(int)` → 切换页面（I/II/III/IV）
- 首次探索节点可获得丹方

### 5.6 时间系统 `TimeManager.cs`
- 实现"下一天"10步结算管线（见 3.3 节）
- `AdvanceDay()` → 执行完整结算管线

### 5.7 资源与角色系统
- `ResourceManager.cs`：5种资源增减/查询，变化时触发事件刷新UI
- `CharacterManager.cs`：角色技能等级（含临时加成）、血量、五行属性、创伤管理、丹药效果计时

---

## 六、UI系统设计

### 6.1 Canvas 层级

```
MainCanvas (Screen Space - Overlay, CanvasScaler 1920x1080)
├── EventSystem (InputSystemUIInputModule)
├── BackgroundLayer (背景层)
├── PanelLayer (面板层, 各功能面板互斥显示)
│   ├── MapPanel          (主地图面板)
│   ├── EventPanel        (事件面板)
│   ├── AlchemyPanel      (炼丹面板 - 核心炼制)
│   ├── ProcessPanel      (加工面板 - 研磨/蒸馏/升炼, 卡牌即工作台)
│   ├── FieldPanel        (药田面板 - 种植/堆肥)
│   ├── AdventurePanel    (冒险面板 - 采药冒险)
│   ├── BlueprintPanel    (图纸库面板)
│   ├── DanPuPanel        (丹谱面板 - 查阅/解锁)
│   └── ResultPanel       (结算面板)
├── CardBarLayer (底部卡牌栏, 始终显示)
├── ResourceBarLayer (右侧资源栏, 事件/炼丹时显示)
├── TopBarLayer (顶部信息栏: 天数/时间)
└── OverlayLayer (最上层: 拖拽中的卡牌/弹窗/遮罩)
    └── DragLayer (拖拽中卡牌的临时父物体)
```

### 6.2 MapPanel（主界面）

```
MapPanel
├── BackgroundImage (红色地毯地图背景)
├── CompassImage (顶部罗盘装饰)
├── MapNodesContainer (六边形"?"节点容器)
├── PathLinesContainer (金色虚线路径)
├── PageSwitcher (左下角罗马数字 I/II/III/IV)
└── NextDayButton ("下一天"结算按钮)
```

### 6.3 EventPanel（事件界面）

```
EventPanel
├── BackgroundImage (山水画背景)
├── TitleBar (顶部标题栏)
├── EventTextPanel (中央事件文本面板)
├── ChoiceButtonContainer (3个选择按钮: 骷髅/交叉剑/勾选)
├── CardSlotContainer (卡牌槽位区)
├── ResourceBar (右侧垂直资源栏)
└── CardBar (底部卡牌栏, 共用)
```

### 6.4 AlchemyPanel（炼丹界面）

```
AlchemyPanel
├── BackgroundImage (暗红色织锦背景)
├── MaterialArea (左侧材料区)
│   ├── RecipeCardSlot (丹方卡槽)
│   ├── MainMaterialSlots (主材槽位 x3, 需加工后材料)
│   └── AuxiliaryMaterialSlots (辅材槽位)
├── CharacterActionArea (中央角色行动区)
├── PropertyComparisonPanel (属性对比面板: 工人金木水火土 vs 丹方需求, 显示总优势值)
├── ResultPanel (右侧炼制结果面板)
├── ActionButtons (底部3按钮: 脚印/X/勾选)
├── FuelDisplay (燃料/丹炉状态)
└── CardBar (底部卡牌栏)
```

### 6.5 ProcessPanel（加工界面 - 卡牌即工作台）

```
ProcessPanel
├── BackgroundImage (工坊背景)
├── WorkbenchCenter (中央工作台区, 工具卡居中显示)
│   └── ToolCardDisplay (工具卡放大显示, 内嵌材料槽位)
├── MaterialPool (左侧待加工材料区)
├── DurationDisplay (加工进度: 剩余天数)
├── ProcessResultPreview (右侧预览: 加工后产出的材料卡)
└── CardBar (底部卡牌栏)
```

核心交互：拖材料卡→悬停工具卡→工具卡槽位高亮→松手→材料嵌入工具卡内部槽位→启动加工→Duration倒计时

### 6.6 FieldPanel（药田界面）

```
FieldPanel
├── BackgroundImage (药田背景)
├── GardenGrid (药田地块网格, 3x2 = 6块地)
│   └── GardenPlot (单个地块: 状态图标+种植内容+倒计时)
├── SeedBagArea (左侧种子卡区域)
├── CompostArea (右侧堆肥物区域)
├── PlotDetailPanel (选中地块详情)
└── CardBar (底部卡牌栏)
```

### 6.7 AdventurePanel（冒险界面）

```
AdventurePanel
├── BackgroundImage (冒险公会背景)
├── AdventureList (左侧冒险地点列表)
│   └── AdventureEntry (图标+名称+偏好属性+Duration)
├── AdventureDetail (右侧详情区)
│   ├── AdventurerSlot (冒险者槽)
│   ├── PotionSlot (药剂槽)
│   ├── CostDisplay (资金/食物消耗)
│   └── StartButton (出发按钮)
├── OngoingAdventures (底部进行中冒险: 倒计时)
└── CardBar (底部卡牌栏)
```

### 6.8 BlueprintPanel（图纸库界面）

```
BlueprintPanel
├── BackgroundImage (书架背景)
├── BlueprintCategoryTabs (顶部标签: 建筑/工具/丹方)
├── BlueprintGrid (蓝图网格, 已学/未学区分)
│   └── BlueprintEntry (图标+名称+材料需求+制造按钮)
├── CraftDetailPanel (选中蓝图详情)
└── CardBar (底部卡牌栏)
```

### 6.9 DanPuPanel（丹谱界面）

```
DanPuPanel
├── BackgroundImage (古籍背景)
├── DanPuCategoryTabs (顶部标签: 丹方/药材/丹药/功能)
├── DanPuEntryList (条目列表)
│   └── DanPuEntryDisplay (条目内容+记录天数+尝试结果)
├── RecipeDetailView (选中丹方详情: 材料需求+属性阈值+最优属性配比)
└── CardBar (底部卡牌栏)
```

### 6.10 卡牌Prefab

```
Card_Base (竖向卡牌)
├── Frame (Image, 边框)
├── Background (Image, 按类型变色)
├── IconArea / PortraitArea (图标/立绘区)
├── TitleText / DescriptionText (TextMeshPro)
├── ElementIcons (金木水火土属性图标容器)
├── VanishCounter (倒计时显示)
├── CardTypeBadge (类型标识)
├── LevelBadge (等级标识)
├── SimpleDragable (脚本组件)
├── CanvasGroup (拖拽时半透明)
└── CardController (脚本组件)
```

卡牌类型差异化渲染：Character 显示立绘+技能+血量+五行属性+创伤标记；Herb 显示草药图标+五行属性+最强属性柱标记；Pill 显示丹药图标+品阶+效果+属性提升；Recipe 显示丹方图标+材料需求+属性阈值；Tool 显示工具图标+等级+内嵌槽位；Seed 显示种子图标+等级+变体+生长天数+产出预览。

---

## 七、命名规范与内容规划

### 7.1 命名规范

| 类别 | 格式 | 示例 |
|------|------|------|
| 工具 | [工具名][等级]级 | 药碾台1级、药碾台2级、蒸馏器1级、丹炉1级 |
| 采药地图 | 采药地图[等级]级-[区域] | 采药地图1级-A、采药地图1级-B、采药地图2级-C |
| 种子 | [等级]级种子[编号] | 1级种子A、1级种子B、2级种子A |
| 药材 | [等级]级药材[编号] | 1级药材A、1级药材B、2级药材A |
| 碎药材 | [等级]级碎药材[编号] | 1级碎药材A、2级碎药材B |
| 属性精华 | [属性]精华[等级] | 金精华1级、火精华2级、木精华1级 |
| 丹药 | [等级]级丹药[名称] | 1级丹药回春丹、2级丹药培元丹、3级丹药九转金丹 |
| 丹方 | 丹方-[丹药名] | 丹方-回春丹、丹方-培元丹、丹方-九转金丹 |
| 图纸 | 图纸-[名称] | 图纸-药碾台2级、图纸-蒸馏器1级、图纸-丹炉2级 |
| 角色 | [角色名] | 炼丹师张三、采药人李四、农夫王五 |
| 灵觉 | 灵觉-[名称] | 灵觉-初识药理、灵觉-丹道入门 |
| 事件 | 事件-[名称] | 事件-上山采药、事件-丹炉试火 |

### 7.2 五行属性命名

| 属性 | 中文名 | 图标色 | 说明 |
|------|--------|--------|------|
| Metal | 金 | 白/银 | 锋利、坚韧，影响切割类加工 |
| Wood | 木 | 绿 | 生长、生机，影响种植和草药 |
| Water | 水 | 蓝 | 流动、润泽，影响蒸馏和萃取 |
| Fire | 火 | 红 | 炽热、变革，影响炼制和升炼 |
| Earth | 土 | 黄 | 稳固、承载，影响种植和堆肥 |

### 7.3 内容规划示例

**工具（升级链）：**
```
药碾台1级 → 药碾台2级 → 药碾台3级
  槽位: 1      槽位: 2      槽位: 3
  药性损失: 30%  药性损失: 20%  药性损失: 10%
  额外产出: 0%   额外产出: 15%  额外产出: 30%

蒸馏器1级 → 蒸馏器2级 → 蒸馏器3级
  槽位: 1      槽位: 2      槽位: 3
  提纯率: 50%   提纯率: 70%   提纯率: 90%

丹炉1级 → 丹炉2级 → 丹炉3级 → 丹炉4级 → 丹炉5级
  成功率加成: 0%  +5%   +10%  +15%  +20%
  槽位: 2      3     4     5     6
```

**采药地图：**
```
采药地图1级-A：山林边缘，偏好金/木属性，产出1级药材/1级种子
采药地图1级-B：溪流旁畔，偏好水/木属性，产出1级药材/1级种子
采药地图1级-C：峭壁矿洞，偏好金/土属性，产出1级药材/矿石
采药地图1级-D：荒野草地，偏好土/火属性，产出1级药材/1级种子
采药地图2级-A：深山密林，偏好木/水属性，产出2级药材/2级种子
采药地图2级-B：火山脚下，偏好火/土属性，产出2级药材/火属性精华
采药地图3级-A：遗迹深处，偏好全属性，产出3级药材/遗迹丹方
```

**种子与药材：**
```
1级种子A → (种植) → 1级药材A（主属性: 木2）
1级种子B → (种植) → 1级药材B（主属性: 水2）
1级种子C → (种植) → 1级药材C（主属性: 土2）
2级种子A → (种植) → 2级药材A（主属性: 木3金1）
2级种子B → (种植) → 2级药材B（主属性: 火3水1）
3级种子A → (种植) → 3级药材A（主属性: 全2，高级材料）
```

**丹方与丹药：**
```
丹方-回春丹 → 1级丹药回春丹（恢复血量，提升木属性，持续3天）
  需求: 1级药材A x2 + 1级碎药材B x1, 木属性阈值: 3
  最强柱: 木≥2, 成功率: 60%

丹方-培元丹 → 2级丹药培元丹（提升全属性1点，持续5天）
  需求: 2级药材A x1 + 2级药材B x1 + 木精华1级 x1, 全属性阈值: 各2
  最强柱: 无限制, 成功率: 45%, 需高级材料(≥2维)

丹方-九转金丹 → 3级丹药九转金丹（大幅提升金属性，持续10天）
  需求: 3级药材A x2 + 金精华2级 x1 + 火精华1级 x1
  金属性阈值: 8, 火属性阈值: 4
  最强柱: 金≥3, 成功率: 30%, 需高级材料(≥2维), 需丹炉3级
```

**丹药分类（9种）：**
```
药用丹(4种): 回春丹(回血)、解毒丹(解毒)、疗伤丹(治创)、凝神丹(提神)
炼丹(1种): 造化丹(提升炼丹成功率)
内丹(3种): 金刚丹(金属性)、青木丹(木属性)、玄水丹(水属性)
动物内丹(1种): 灵兽内丹(全属性提升，高风险)
修行丹(多级): 1-5级修行丹(提升修为等级)
植物丹(多级): 1-3级植物丹(提升对应植物属性)
```

**灵觉序列：**
```
灵觉-初识药理 → 解锁: 采药地图1级、1级种子A、丹方-回春丹
灵觉-丹道入门 → 解锁: 药碾台1级图纸、丹炉1级、丹方-培元丹
灵觉-五行相生 → 解锁: 蒸馏器1级图纸、属性精华系统、2级采药地图
灵觉-丹成之道 → 解锁: 丹炉2级图纸、高级丹方、丹谱全部功能
```

---

## 八、实现步骤

### 阶段 1：项目搭建与基础设施
1. 创建目录结构
2. 导入 DOTween
3. 配置 EventSystem：替换为 InputSystemUIInputModule
4. 创建主 Canvas，建立 UI 层级架构

### 阶段 2：核心框架
1. 实现事件总线：GameEvent.cs, GameEventT.cs, GameEventListener.cs
2. 实现状态机：GameStateMachine.cs, GameStateBase.cs, 11个状态类, GameController.cs
3. 实现 ServiceLocator.cs，各 Manager 在 Awake() 中注册
4. 实现 GameBootstrap.cs 场景启动器
- 验证：场景运行后 GameController 正常初始化，状态机切换到 BootState

### 阶段 3：数据模型与 ScriptableObject
1. 编写所有数据定义类（16个数据类 + Enums）
2. 编写编辑器扩展
3. 创建最小可玩数据集：3张角色卡、5张药材卡、3张种子卡、2张丹方、3个丹药、3个事件、6个地图节点、2个灵觉、3张图纸、2个工具
- 验证：Inspector 中能正常创建和编辑所有 SO 资产

### 阶段 4：卡牌系统
1. 实现卡牌实例与管理器：CardInstance.cs, CardManager.cs
2. 实现交互系统：SimpleDragable.cs, DropDelegater.cs, PointerClickDelegater.cs
3. 制作卡牌 Prefab（Card_Base.prefab）
4. 实现卡牌视觉控制器：CardController.cs
5. 实现槽位系统：CardSlotController.cs（磁性吸附）
6. 实现手牌栏：HandCardsController.cs
- 验证：手牌栏显示卡牌，可拖拽、可放入槽位、可回弹，翻页正常

### 阶段 5：主地图系统
1. 实现 MapManager.cs, MapController.cs
2. 制作地图节点 Prefab（六边形"?"标记）
3. 制作 MapPanel.prefab
4. 实现 MapPanelController.cs
- 验证：地图显示多个"?"节点，点击触发事件，翻页正常，"下一天"按钮触发天数增加

### 阶段 6：事件/仪式系统
1. 实现 EventManager.cs, DiceController.cs
2. 制作 EventPanel.prefab
3. 实现 RitePanelController.cs, RiteController.cs
4. 实现结果面板：RiteResultPanelController.cs
5. 实现资源栏：ResourceBarUI.cs
- 验证：完整走通"地图点击→事件界面→选择→判定→结果→返回地图"

### 阶段 7：炼丹系统（多阶段加工）
1. 实现属性对比器：PropertyComparator.cs（金木水火土逐项对比）
2. 实现炼丹管理器：AlchemyManager.cs
3. 制作 AlchemyPanel.prefab（丹炉工作台布局+属性对比面板）
4. 实现 AlchemyPanelController.cs
5. 实现多阶段加工：ProcessController.cs（研磨/蒸馏/升炼，Duration）
6. 制作 ProcessPanel.prefab（卡牌即工作台：工具卡居中，材料卡嵌入）
7. 实现工具管理器：ToolManager.cs（工具卡牌管理、品质升级链）
8. 实现药田系统：FieldController.cs（种植+堆肥双模式）
9. 制作 FieldPanel.prefab
- 验证：完整走通"选丹方→放材料→属性对比→炼制→获得丹药/丹渣"；研磨可改变材料属性；药田可种植收获+堆肥

### 阶段 8：图纸、丹谱与冒险系统
1. 实现图纸管理器：BlueprintManager.cs
2. 制作 BlueprintPanel.prefab
3. 实现丹谱管理器：DanPuManager.cs
4. 制作 DanPuPanel.prefab
5. 实现灵觉管理器：LingJueManager.cs
6. 实现冒险管理器：AdventureManager.cs
7. 制作 AdventurePanel.prefab
8. 实现 WorkbenchCardUI.cs（工具卡嵌入式槽位渲染）
- 验证：图纸可学习/制造；丹谱可记录/查阅；灵觉可触发/完成/解锁；冒险可出发/结算/获得奖励

### 阶段 9：时间与全局系统
1. 实现 TimeManager.cs（"下一天"10步结算管线）
2. 实现 ResourceManager.cs, CharacterManager.cs
3. 实现 InventoryManager.cs
4. 集成辅助系统：药物食物→临时提升属性→缩短动作时间；书籍残页→发现丹方/图纸
- 验证："下一天"按10步顺序执行，药田/加工/冒险随天数推进

### 阶段 10：全流程串联与内容填充
1. 串联完整游戏循环：灵觉入门→地图→采集→获得材料→种田/加工→炼丹→获得丹药→服丹→下一天→新事件
2. 扩充内容数据：10+事件、5+丹方、10+药材、5+种子、3-5角色、12+地图节点(4页)、4+灵觉、6+工具、6+图纸、3+冒险
3. 实现定时/随机事件
4. 实现胜负条件（角色全灭/达成炼丹目标/时间耗尽）

### 阶段 11：打磨与优化
1. 动画与反馈：卡牌拖拽/吸附/回弹、面板淡入淡出、资源数字跳动、炼丹特效、骰子动画
2. 音效占位
3. 新手引导（首次操作高亮提示）
4. 数据持久化（可选）：JsonUtility 序列化存档/读档

---

## 九、验证方法

### 9.1 各阶段验证清单

| 阶段 | 验证方法 | 通过标准 |
|------|---------|---------|
| 阶段1 | 运行场景检查控制台 | 无报错，EventSystem 使用 InputSystemUIInputModule |
| 阶段2 | Debug.Log 验证状态机 | 状态切换日志正确，ServiceLocator 能获取各 Manager |
| 阶段3 | Inspector 创建 SO 资产 | 所有字段正确显示，可编辑，无序列化错误 |
| 阶段4 | 拖拽测试 | 卡牌可拖拽、可放入槽位、可回弹、翻页正常 |
| 阶段5 | 地图交互测试 | 节点可点击、翻页正常、路径显示正确 |
| 阶段6 | 事件流程测试 | 完整走通"选事件→选择→判定→结果→返回" |
| 阶段7 | 炼丹+加工流程测试 | 完整走通"选丹方→放材料→属性对比→炼制→结果"；研磨/蒸馏/升炼可改变材料属性；药田可种植收获+堆肥 |
| 阶段8 | 图纸+丹谱+冒险测试 | 图纸可学习/制造；丹谱可记录/查阅；灵觉可触发/完成；冒险可出发→结算→获得奖励 |
| 阶段9 | 结算管线测试 | "下一天"按10步顺序执行，药田/加工/冒险随天数推进 |
| 阶段10 | 全流程测试 | 能持续游玩 10+ 天，完整循环跑通 |
| 阶段11 | 体验测试 | 动画流畅，无卡顿，操作反馈清晰 |

### 9.2 关键验证场景

**场景 1 - 卡牌拖拽全链路：** 手牌栏5张卡→拖药材卡到事件槽位→吸附成功→拖不满足条件的卡→拒绝回弹→翻页切换

**场景 2 - 事件判定全链路：** 地图点击"采药地图1级-A"节点→显示事件文本和3个选择按钮→放入角色卡→选择"交叉剑"→掷骰子判定→显示结果→资源栏和手牌栏更新

**场景 3 - 炼丹全链路：** 进入炼丹界面→拖入"丹方-回春丹"→拖入2张"1级药材A"→拖入角色→属性对比面板显示金木水火土对比→点击"勾选"→判定→成功获得"1级丹药回春丹"/失败获得丹渣→丹谱记录

**场景 4 - "下一天"结算：** 倒计时卡牌(2天)→点"下一天"→倒计时变1→再点→归零触发事件→药田生长→加工Duration-1→冒险Duration-1→临时加成减少

**场景 5 - 多阶段加工全链路：** 拖"1级药材A"到"药碾台1级"工作台槽位→研磨启动(Duration1天)→点"下一天"→获得"1级碎药材A"(属性降低但带Crushed标签)→拖碎药材到"蒸馏器1级"→蒸馏启动→点"下一天"→获得"木精华1级"→拖精华到"丹炉2级"+选丹方+拖入角色→属性对比判定→获得丹药

**场景 6 - 工具升级链：** 获得图纸"图纸-药碾台2级"→在图纸库面板查看材料需求→收集材料→点击制造→消耗材料获得"药碾台2级"工具卡→用2级药碾台研磨(对比1级：药性损失更少，额外产出概率更高)

**场景 7 - 药田种植+堆肥：** 拖"1级种子A"到药田地块→拖农夫卡加速→点"下一天"→生长倒计时减少→归零后收获"1级药材A"→拖丹渣(可分解物)到空地块→堆肥产出属性肥料→下次种植该地块的药材获得属性加成

**场景 8 - 采药冒险：** 进入冒险面板→选择"采药地图1级-A"冒险→拖入冒险者卡+药剂卡→消耗资金和食物→冒险开始(Duration3天)→点3次"下一天"→冒险结算：冒险者属性 vs 偏好属性对比→按权重选择结果→获得奖励

**场景 9 - 灵觉入门：** 游戏开始→触发"灵觉-初识药理"→显示灵觉事件文本→完成灵觉→解锁"采药地图1级"地图节点、"1级种子A"、"丹方-回春丹"→丹谱记录新发现

**场景 10 - 丹谱查阅：** 炼制丹药成功→丹谱自动记录→打开丹谱面板→查看"丹方-回春丹"条目→显示材料需求、属性阈值、尝试次数、成功/失败记录

---

## 十、设计决策

### 10.1 技术决策
1. 单场景多面板：卡牌可跨界面拖拽，状态保持连贯，切换无加载延迟
2. ScriptableObject 数据驱动：卡牌/事件/丹方/角色数据均为 SO 资产，数据与逻辑分离
3. ScriptableObject 事件总线：模块间解耦通信
4. DOTween 动画：简化动画代码
5. New Input System：项目已配置，UGUI 接口通过 InputSystemUIInputModule 正常工作
6. 占位素材策略：纯色 Image + TextMeshPro 文字标注占位

### 10.2 游戏设计决策
1. 金木水火土属性系统：五行相生相克，属性对比判定为核心机制
2. "1人+1物"成本模型：所有操作消耗一个角色和一个资源
3. 5种资源：红宝石(灵石)/红心(体力)/绿叶(药性)/紫漩涡(灵力)/绿螺旋(修为)
4. 多阶段加工流水线：材料需经研磨→蒸馏→升炼才能用于核心炼制，每阶段有Duration
5. 工具卡即工作台：工具卡本身有槽位，材料嵌入工具卡内部，保持卡牌操作统一性
6. 图纸解锁制造：工具/建筑/丹方需先发现图纸才能制造
7. 药田双模式：种植+堆肥，丹渣/废料可堆肥为属性肥料，形成资源循环
8. 采药冒险：高风险高回报的资源获取途径，不同冒险偏好不同属性
9. 灵觉系统：不可重复的学习入口，驱动游戏进程和内容解锁
10. 丹谱系统：记录尝试历史，是配方解锁和知识积累的载体
11. 创伤系统：受伤增强属性但累积过多致命，丹药可治愈
12. 丹药效果：临时提升某属性，同时只能服一种，效果随天数消退
13. 失败有补偿：炼丹失败产出丹渣（可堆肥回药田）
14. 统一命名规范：所有物品按"名称+等级+变体"格式命名，清晰直观
