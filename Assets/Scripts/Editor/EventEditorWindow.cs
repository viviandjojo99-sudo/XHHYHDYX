#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using XHHY.Game.Data;
using XHHY.Game.Runtime;
using XHHY.Game.Settlement;

namespace XHHY.Game.EditorTools
{
    public class EventEditorWindow : EditorWindow
    {
        Vector2 _leftScroll;
        Vector2 _rightScroll;
        string _search = "";
        ScriptableObject _selected;
        int _tab;
        readonly string[] _tabs = { "基本", "卡槽", "需求", "出现条件", "结果分支", "模拟" };

        // 模拟
        readonly List<CardData> _simCards = new List<CardData>();
        bool _simSwitch;
        string _simSwitchKey = "jialila_corpse_done";

        [MenuItem("炼丹游戏/事件编辑器")]
        public static void Open() => GetWindow<EventEditorWindow>("事件编辑器");

        void OnGUI()
        {
            EditorGUILayout.BeginHorizontal();
            DrawLeft();
            DrawRight();
            EditorGUILayout.EndHorizontal();
        }

        void DrawLeft()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(260));
            _search = EditorGUILayout.TextField("搜索", _search);
            if (GUILayout.Button("新建事件"))
            {
                var ev = CreateInstance<EventData>();
                ev.eventId = "event_new";
                ev.title = "新事件";
                AssetDatabase.CreateAsset(ev, $"Assets/Data/Events/Event_{Guid.NewGuid():N}.asset");
                _selected = ev;
            }
            if (GUILayout.Button("新建公共事件"))
            {
                var ev = CreateInstance<CommonEventData>();
                ev.commonEventId = "common_new";
                ev.title = "新公共事件";
                AssetDatabase.CreateAsset(ev, $"Assets/Data/CommonEvents/Common_{Guid.NewGuid():N}.asset");
                _selected = ev;
            }

            _leftScroll = EditorGUILayout.BeginScrollView(_leftScroll);
            DrawAssetList<EventData>("事件");
            DrawAssetList<CommonEventData>("公共事件");
            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        void DrawAssetList<T>(string label) where T : ScriptableObject
        {
            EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeof(T).Name))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<T>(path);
                if (asset == null) continue;
                string name = asset.name;
                if (asset is EventData ed && !string.IsNullOrEmpty(ed.title)) name = ed.title;
                if (asset is CommonEventData ce && !string.IsNullOrEmpty(ce.title)) name = ce.title;
                if (!string.IsNullOrEmpty(_search) && name.IndexOf(_search, StringComparison.OrdinalIgnoreCase) < 0)
                    continue;
                if (GUILayout.Toggle(_selected == asset, name, "Button"))
                    _selected = asset;
            }
        }

        void DrawRight()
        {
            EditorGUILayout.BeginVertical();
            if (_selected == null)
            {
                EditorGUILayout.HelpBox("选择左侧事件。", MessageType.Info);
                EditorGUILayout.EndVertical();
                return;
            }

            _tab = GUILayout.Toolbar(_tab, _tabs);
            _rightScroll = EditorGUILayout.BeginScrollView(_rightScroll);

            if (_selected is EventData ev)
                DrawEvent(ev);
            else if (_selected is CommonEventData common)
                DrawCommon(common);

            EditorGUILayout.EndScrollView();
            if (GUILayout.Button("保存"))
            {
                EditorUtility.SetDirty(_selected);
                AssetDatabase.SaveAssets();
            }
            EditorGUILayout.EndVertical();
        }

        void DrawEvent(EventData ev)
        {
            switch (_tab)
            {
                case 0:
                    ev.eventId = EditorGUILayout.TextField("eventId", ev.eventId);
                    ev.title = EditorGUILayout.TextField("标题", ev.title);
                    ev.openingText = EditorGUILayout.TextArea(ev.openingText, GUILayout.MinHeight(60));
                    ev.previewOutcomeText = EditorGUILayout.TextArea(ev.previewOutcomeText, GUILayout.MinHeight(40));
                    ev.repeatable = EditorGUILayout.Toggle("可重复", ev.repeatable);
                    ev.autoStartOnRound = EditorGUILayout.Toggle("回合自动弹出", ev.autoStartOnRound);
                    break;
                case 1:
                    DrawSlots(ev);
                    break;
                case 2:
                    DrawRequirements(ev);
                    break;
                case 3:
                    DrawConditionList(ev.appearConditions, () => ev.appearConditions ??= new List<SettlementCondition>());
                    break;
                case 4:
                    DrawOutcomes(ev);
                    break;
                case 5:
                    DrawSimulate(ev);
                    break;
            }
        }

        void DrawCommon(CommonEventData ev)
        {
            ev.commonEventId = EditorGUILayout.TextField("id", ev.commonEventId);
            ev.title = EditorGUILayout.TextField("标题", ev.title);
            ev.trigger = (CommonEventTrigger)EditorGUILayout.EnumPopup("触发器", ev.trigger);
            ev.triggerFilter = EditorGUILayout.TextField("过滤", ev.triggerFilter);
            ev.once = EditorGUILayout.Toggle("只一次", ev.once);
            EditorGUILayout.LabelField("条件", EditorStyles.boldLabel);
            DrawConditionList(ev.conditions, () => ev.conditions ??= new List<SettlementCondition>());
            EditorGUILayout.LabelField("命令", EditorStyles.boldLabel);
            DrawCommandList(ev.commands, () => ev.commands ??= new List<SettlementCommand>());
        }

        void DrawSlots(EventData ev)
        {
            ev.slots ??= new List<EventSlotData>();
            for (int i = 0; i < ev.slots.Count; i++)
            {
                var s = ev.slots[i];
                EditorGUILayout.BeginVertical("box");
                s.slotId = EditorGUILayout.TextField("slotId", s.slotId);
                s.acceptType = (CardTypeDefinition)EditorGUILayout.ObjectField("acceptType", s.acceptType, typeof(CardTypeDefinition), false);
                s.acceptGroup = EditorGUILayout.TextField("acceptGroup", s.acceptGroup);
                s.required = EditorGUILayout.Toggle("必填", s.required);
                s.anchoredPosition = EditorGUILayout.Vector2Field("位置", s.anchoredPosition);
                if (GUILayout.Button("删除槽"))
                {
                    ev.slots.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("+ 卡槽"))
                ev.slots.Add(new EventSlotData { slotId = "slot_" + ev.slots.Count, required = true });
        }

        void DrawRequirements(EventData ev)
        {
            ev.requirements ??= new List<AttributeRequirement>();
            for (int i = 0; i < ev.requirements.Count; i++)
            {
                var r = ev.requirements[i];
                EditorGUILayout.BeginHorizontal();
                r.attributeId = EditorGUILayout.TextField(r.attributeId);
                r.required = EditorGUILayout.IntField(r.required);
                if (GUILayout.Button("x", GUILayout.Width(24)))
                {
                    ev.requirements.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }
            if (GUILayout.Button("+ 需求"))
                ev.requirements.Add(new AttributeRequirement { attributeId = "fire", required = 1 });
        }

        void DrawOutcomes(EventData ev)
        {
            ev.outcomes ??= new List<EventOutcome>();
            for (int i = 0; i < ev.outcomes.Count; i++)
            {
                var o = ev.outcomes[i];
                EditorGUILayout.BeginVertical("box");
                o.outcomeId = EditorGUILayout.TextField("id", o.outcomeId);
                o.previewMode = (PreviewMode)EditorGUILayout.EnumPopup("预览模式", o.previewMode);
                o.previewText = EditorGUILayout.TextField("预览", o.previewText);
                o.resultText = EditorGUILayout.TextField("结果", o.resultText);
                EditorGUILayout.LabelField("条件");
                DrawConditionList(o.conditions, () => o.conditions ??= new List<SettlementCondition>());
                EditorGUILayout.LabelField("命令");
                DrawCommandList(o.commands, () => o.commands ??= new List<SettlementCommand>());
                if (GUILayout.Button("删除分支"))
                {
                    ev.outcomes.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndVertical();
            }
            if (GUILayout.Button("+ 分支"))
                ev.outcomes.Add(new EventOutcome { outcomeId = "outcome_" + ev.outcomes.Count });
        }

        void DrawConditionList(List<SettlementCondition> list, Action ensure)
        {
            ensure();
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(list[i]?.EditorLabel ?? "(null)");
                if (GUILayout.Button("x", GUILayout.Width(24)))
                {
                    list.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
                if (list[i] != null)
                {
                    var so = new SerializedObject(_selected);
                    // 简单字段编辑：用 EditorGUI 反射常见字段
                    DrawConditionFields(list[i]);
                }
            }
            if (GUILayout.Button("+ 条件"))
            {
                var menu = new GenericMenu();
                void Add<T>(string label) where T : SettlementCondition, new()
                {
                    menu.AddItem(new GUIContent(label), false, () =>
                    {
                        ensure();
                        list.Add(new T());
                    });
                }
                Add<CondRequirementTier>("分档");
                Add<CondAttributeTotal>("属性合计");
                Add<CondSlotHasCard>("槽有牌");
                Add<CondCardLocation>("卡牌位置");
                Add<CondCardAlive>("卡牌存活");
                Add<CondSwitchIs>("开关");
                Add<CondVariableCompare>("变量");
                Add<CondEventCompleted>("事件完成");
                Add<CondHasCard>("拥有卡牌");
                Add<CondRecipeQuality>("炼丹品级");
                menu.ShowAsContext();
            }
        }

        void DrawConditionFields(SettlementCondition c)
        {
            switch (c)
            {
                case CondAttributeTotal a:
                    a.attributeId = EditorGUILayout.TextField("属性", a.attributeId);
                    a.op = (CondAttributeTotal.Op)EditorGUILayout.EnumPopup("比较", a.op);
                    a.value = EditorGUILayout.IntField("值", a.value);
                    break;
                case CondRequirementTier t:
                    t.tier = (RequirementTier)EditorGUILayout.EnumPopup("分档", t.tier);
                    t.compare = (CondRequirementTier.Compare)EditorGUILayout.EnumPopup("比较", t.compare);
                    break;
                case CondSwitchIs s:
                    s.switchKey = EditorGUILayout.TextField("开关", s.switchKey);
                    s.expected = EditorGUILayout.Toggle("期望", s.expected);
                    break;
                case CondCardAlive ca:
                    ca.cardId = EditorGUILayout.TextField("cardId", ca.cardId);
                    ca.alive = EditorGUILayout.Toggle("存活", ca.alive);
                    break;
                case CondCardLocation cl:
                    cl.cardId = EditorGUILayout.TextField("cardId", cl.cardId);
                    cl.location = (CardLocation)EditorGUILayout.EnumPopup("位置", cl.location);
                    break;
                case CondEventCompleted ec:
                    ec.eventId = EditorGUILayout.TextField("eventId", ec.eventId);
                    ec.completed = EditorGUILayout.Toggle("已完成", ec.completed);
                    break;
                case CondRecipeQuality rq:
                    rq.quality = (RecipeQuality)EditorGUILayout.EnumPopup("品级", rq.quality);
                    break;
            }
        }

        void DrawCommandList(List<SettlementCommand> list, Action ensure)
        {
            ensure();
            for (int i = 0; i < list.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField(list[i]?.EditorLabel ?? "(null)");
                if (GUILayout.Button("x", GUILayout.Width(24)))
                {
                    list.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
                if (list[i] != null)
                    DrawCommandFields(list[i]);
            }
            if (GUILayout.Button("+ 命令"))
            {
                var menu = new GenericMenu();
                void Add<T>(string label) where T : SettlementCommand, new()
                {
                    menu.AddItem(new GUIContent(label), false, () =>
                    {
                        ensure();
                        list.Add(new T());
                    });
                }
                Add<CmdConsumeCard>("消耗卡牌");
                Add<CmdGrantCard>("获得卡牌");
                Add<CmdMoveCard>("移动卡牌");
                Add<CmdKillCard>("击杀卡牌");
                Add<CmdShowResult>("显示结果");
                Add<CmdSetSwitch>("设置开关");
                Add<CmdAddVariable>("修改变量");
                Add<CmdActivateEvent>("激活事件标记");
                Add<CmdOpenEvent>("打开事件");
                Add<CmdShowChoices>("显示选项");
                Add<CmdGrantRecipeOutput>("发放炼丹产物");
                Add<CmdMarkEventCompleted>("标记事件完成");
                menu.ShowAsContext();
            }
        }

        void DrawCommandFields(SettlementCommand c)
        {
            switch (c)
            {
                case CmdGrantCard g:
                    g.card = (CardData)EditorGUILayout.ObjectField("卡牌", g.card, typeof(CardData), false);
                    g.count = EditorGUILayout.IntField("数量", g.count);
                    g.location = (CardLocation)EditorGUILayout.EnumPopup("位置", g.location);
                    break;
                case CmdSetSwitch s:
                    s.switchKey = EditorGUILayout.TextField("开关", s.switchKey);
                    s.value = EditorGUILayout.Toggle("值", s.value);
                    break;
                case CmdMoveCard m:
                    m.cardId = EditorGUILayout.TextField("cardId", m.cardId);
                    m.from = (CardLocation)EditorGUILayout.EnumPopup("从", m.from);
                    m.to = (CardLocation)EditorGUILayout.EnumPopup("到", m.to);
                    break;
                case CmdShowResult r:
                    r.text = EditorGUILayout.TextField("文本", r.text);
                    break;
                case CmdOpenEvent o:
                    o.eventData = (EventData)EditorGUILayout.ObjectField("事件", o.eventData, typeof(EventData), false);
                    break;
                case CmdMarkEventCompleted me:
                    me.eventId = EditorGUILayout.TextField("eventId", me.eventId);
                    break;
            }
        }

        void DrawSimulate(EventData ev)
        {
            EditorGUILayout.HelpBox("选择投入卡牌，查看命中分支与属性差值。", MessageType.Info);
            if (GUILayout.Button("添加模拟卡牌"))
                EditorGUIUtility.ShowObjectPicker<CardData>(null, false, "", 9901);
            if (Event.current.commandName == "ObjectSelectorUpdated" && EditorGUIUtility.GetObjectPickerControlID() == 9901)
            {
                var card = EditorGUIUtility.GetObjectPickerObject() as CardData;
                if (card != null)
                    _simCards.Add(card);
            }

            for (int i = 0; i < _simCards.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                _simCards[i] = (CardData)EditorGUILayout.ObjectField(_simCards[i], typeof(CardData), false);
                if (GUILayout.Button("x", GUILayout.Width(24)))
                {
                    _simCards.RemoveAt(i);
                    break;
                }
                EditorGUILayout.EndHorizontal();
            }

            _simSwitchKey = EditorGUILayout.TextField("模拟开关", _simSwitchKey);
            _simSwitch = EditorGUILayout.Toggle("开关值", _simSwitch);

            var provided = new Dictionary<string, int>();
            var instances = new List<CardInstance>();
            for (int i = 0; i < _simCards.Count; i++)
            {
                if (_simCards[i] == null) continue;
                var inst = CardInstance.Create(_simCards[i], CardLocation.Slot, 1);
                instances.Add(inst);
                if (_simCards[i].attributes == null) continue;
                foreach (var a in _simCards[i].attributes)
                {
                    if (a == null) continue;
                    provided.TryGetValue(a.attributeId, out int v);
                    provided[a.attributeId] = v + a.value;
                }
            }

            var tier = GameState.EvaluateTier(ev.ResolveRequirements(), provided);
            EditorGUILayout.LabelField("分档", tier.ToString());
            foreach (var r in ev.ResolveRequirements())
            {
                provided.TryGetValue(r.attributeId, out int cur);
                EditorGUILayout.LabelField($"{r.attributeId}: {cur}/{r.required} 差 {cur - r.required}");
            }

            var state = new GameState();
            state.SetSwitch(_simSwitchKey, _simSwitch);
            var ctx = new SettlementContext
            {
                State = state,
                Event = ev,
                ProvidedAttributes = provided,
                Tier = tier
            };
            for (int i = 0; i < instances.Count; i++)
                ctx.SlotOccupants["sim_" + i] = instances[i];

            var outcome = OutcomeResolver.Resolve(ev, ctx);
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("命中分支", outcome?.outcomeId ?? "(无)");
            EditorGUILayout.LabelField("预览", outcome?.previewText ?? ev.previewOutcomeText);
            EditorGUILayout.LabelField("结果", outcome?.resultText ?? "");
        }
    }
}
#endif
