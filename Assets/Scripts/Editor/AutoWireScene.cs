#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using XHHY.Game;
using XHHY.Game.Data;
using XHHY.Game.Map;
using XHHY.Game.UI;

namespace XHHY.Game.EditorTools
{
    public static class AutoWireScene
    {
        const string CardPrefabPath = "Assets/Prefab/Card.prefab";
        const string CardSlotPrefabPath = "Assets/Prefab/CardSlot.prefab";
        const string HandCardTypePrefabPath = "Assets/Prefab/HandCardType.prefab";
        const string EventAssetPath = "Assets/Data/Events/Event_上山采药.asset";
        const string DeckPath = "Assets/Data/Cards/HandDeckConfig.asset";
        const string CatalogPath = "Assets/Data/CardTypes/CardTypeCatalog.asset";

        [MenuItem("炼丹游戏/自动挂接场景与预制体")]
        public static void Run()
        {
            WireCardPrefab();
            WireCardSlotPrefab();
            WireHandCardTypePrefab();
            WireScene();
            UpdateSampleEventAttributes();
            AssetDatabase.SaveAssets();
            EditorSceneManager.MarkAllScenesDirty();
            EditorSceneManager.SaveOpenScenes();
            Debug.Log("自动挂接完成：Card/CardSlot/HandCardType 预制体 + SampleScene 引用已写好。");
        }

        static void WireCardPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CardPrefabPath);
            var view = root.GetComponent<HandCardView>() ?? root.AddComponent<HandCardView>();
            if (root.GetComponent<CanvasGroup>() == null)
                root.AddComponent<CanvasGroup>();
            if (root.GetComponent<XHHY.Game.Interaction.HandCardDrag>() == null)
                root.AddComponent<XHHY.Game.Interaction.HandCardDrag>();

            // 子 Graphic 不抢射线，根 Image 负责拖拽
            foreach (var g in root.GetComponentsInChildren<Graphic>(true))
            {
                if (g != null)
                    g.raycastTarget = g.gameObject == root;
            }

            var name = root.transform.Find("CardNameFrame/CardName")?.GetComponent<TMP_Text>();
            var icon = root.transform.Find("CardIcon")?.GetComponent<Image>();
            var so = new SerializedObject(view);
            so.FindProperty("nameText").objectReferenceValue = name;
            so.FindProperty("iconImage").objectReferenceValue = icon;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, CardPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void WireCardSlotPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(CardSlotPrefabPath);
            var slot = root.GetComponent<EventCardSlot>() ?? root.AddComponent<EventCardSlot>();
            var typeIcon = root.transform.Find("CardType")?.GetComponent<Image>();
            var so = new SerializedObject(slot);
            so.FindProperty("typeIcon").objectReferenceValue = typeIcon;
            so.FindProperty("cardAnchor").objectReferenceValue = root.transform;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, CardSlotPrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void WireHandCardTypePrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(HandCardTypePrefabPath);
            var item = root.GetComponent<HandCardTagItem>() ?? root.AddComponent<HandCardTagItem>();
            var icon = root.transform.Find("CardTypeIcon")?.GetComponent<Image>()
                       ?? root.GetComponentInChildren<Image>();
            var so = new SerializedObject(item);
            so.FindProperty("iconImage").objectReferenceValue = icon;
            so.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, HandCardTypePrefabPath);
            PrefabUtility.UnloadPrefabContents(root);
        }

        static void WireScene()
        {
            var eventPanelGo = GameObject.Find("EventPanel");
            var handTrayGo = GameObject.Find("HandTray");
            var handCardGo = GameObject.Find("HandCard");
            var handTagGo = GameObject.Find("HandCardTag");
            var bootstrapGo = GameObject.Find("Bootstrap") ?? new GameObject("Bootstrap");
            var markersRoot = GameObject.Find("EventMarkers");

            var cardPrefab = AssetDatabase.LoadAssetAtPath<HandCardView>(CardPrefabPath);
            var slotPrefab = AssetDatabase.LoadAssetAtPath<EventCardSlot>(CardSlotPrefabPath);
            var tagPrefab = AssetDatabase.LoadAssetAtPath<HandCardTagItem>(HandCardTypePrefabPath);
            var deck = AssetDatabase.LoadAssetAtPath<HandDeckConfig>(DeckPath);
            var catalog = AssetDatabase.LoadAssetAtPath<CardTypeCatalog>(CatalogPath);
            var sampleEvent = AssetDatabase.LoadAssetAtPath<EventData>(EventAssetPath);

            // HandTray
            var handTray = handTrayGo.GetComponent<HandTrayController>() ?? handTrayGo.AddComponent<HandTrayController>();
            var handTraySo = new SerializedObject(handTray);
            handTraySo.FindProperty("handCardContainer").objectReferenceValue = handCardGo.transform;
            handTraySo.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
            handTraySo.FindProperty("deckConfig").objectReferenceValue = deck;
            var preferred = handTraySo.FindProperty("preferredSpacing");
            if (preferred != null) preferred.floatValue = 148f;
            var minSp = handTraySo.FindProperty("minSpacing");
            if (minSp != null) minSp.floatValue = 36f;
            handTraySo.ApplyModifiedPropertiesWithoutUndo();

            // HandTray / HandCard 必须挡射线，否则点手牌区会拖地图
            void ForceRaycast(GameObject go)
            {
                if (go == null) return;
                var img = go.GetComponent<Image>();
                if (img == null) img = go.AddComponent<Image>();
                var c = img.color;
                if (c.a < 0.01f) { c.a = 0.01f; img.color = c; }
                img.raycastTarget = true;
            }
            ForceRaycast(handTrayGo);
            ForceRaycast(handCardGo);
            handTrayGo.transform.SetAsLastSibling();

            // 主相机平移缩放
            var cam = Camera.main;
            if (cam != null && cam.GetComponent<CameraPanZoom>() == null)
                Undo.AddComponent<CameraPanZoom>(cam.gameObject);

            // HandCardTag
            var tagBar = handTagGo.GetComponent<HandCardTagBar>() ?? handTagGo.AddComponent<HandCardTagBar>();
            var tagSo = new SerializedObject(tagBar);
            tagSo.FindProperty("catalog").objectReferenceValue = catalog;
            tagSo.FindProperty("tagContainer").objectReferenceValue = handTagGo.GetComponent<RectTransform>();
            tagSo.FindProperty("tagItemPrefab").objectReferenceValue = tagPrefab;
            tagSo.FindProperty("handTray").objectReferenceValue = handTray;
            tagSo.ApplyModifiedPropertiesWithoutUndo();

            // Ensure tag items on children
            foreach (Transform child in handTagGo.transform)
            {
                if (child.GetComponent<HandCardTagItem>() == null)
                    child.gameObject.AddComponent<HandCardTagItem>();
            }

            // Event panel pieces
            var event1 = eventPanelGo.transform.Find("Event1");
            var eventBg = event1?.Find("EventBg")?.GetComponent<Image>();
            var eventName = event1?.Find("NarrativePanel/EventNameFrame/EventName")?.GetComponent<TMP_Text>();
            var eventContent = event1?.Find("NarrativePanel/EventContent")?.GetComponent<TMP_Text>();
            var slotPanel = event1?.Find("CardSlotPanel")?.GetComponent<RectTransform>();
            var dimmer = eventPanelGo.transform.Find("Dimmer")?.gameObject;
            var attributePanel = eventPanelGo.transform.Find("AttributePanel");
            var actions = eventPanelGo.transform.Find("Actions");

            EventNarrativeView narrative = null;
            if (eventContent != null)
            {
                narrative = eventContent.GetComponent<EventNarrativeView>()
                            ?? eventContent.gameObject.AddComponent<EventNarrativeView>();
                var narrSo = new SerializedObject(narrative);
                narrSo.FindProperty("contentText").objectReferenceValue = eventContent;
                narrSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // Attribute summary text: create if missing
            EventAttributeView attrView = null;
            if (attributePanel != null)
            {
                attrView = attributePanel.GetComponent<EventAttributeView>()
                           ?? attributePanel.gameObject.AddComponent<EventAttributeView>();
                var attrText = attributePanel.GetComponentInChildren<TMP_Text>();
                if (attrText == null)
                {
                    var textGo = new GameObject("AttributeSummary", typeof(RectTransform), typeof(TextMeshProUGUI));
                    textGo.transform.SetParent(attributePanel, false);
                    var rt = textGo.GetComponent<RectTransform>();
                    rt.anchorMin = Vector2.zero;
                    rt.anchorMax = Vector2.one;
                    rt.offsetMin = new Vector2(8, 8);
                    rt.offsetMax = new Vector2(-8, -8);
                    attrText = textGo.GetComponent<TextMeshProUGUI>();
                    attrText.fontSize = 18;
                    attrText.color = Color.white;
                    attrText.enableWordWrapping = true;
                }

                var attrSo = new SerializedObject(attrView);
                attrSo.FindProperty("summaryText").objectReferenceValue = attrText;
                attrSo.ApplyModifiedPropertiesWithoutUndo();
            }

            Button confirmBtn = null;
            Button cancelBtn = null;
            if (actions != null)
            {
                foreach (var btn in actions.GetComponentsInChildren<Button>(true))
                {
                    if (btn.name.Contains("Confirm"))
                        confirmBtn = btn;
                    else if (btn.name.Contains("Cancel"))
                        cancelBtn = btn;
                }

                // fallback by order
                if (confirmBtn == null || cancelBtn == null)
                {
                    var btns = actions.GetComponentsInChildren<Button>(true);
                    if (btns.Length >= 2)
                    {
                        confirmBtn ??= btns[0];
                        cancelBtn ??= btns[1];
                    }
                }
            }

            var panel = eventPanelGo.GetComponent<EventPanelController>()
                        ?? eventPanelGo.AddComponent<EventPanelController>();
            var panelSo = new SerializedObject(panel);
            panelSo.FindProperty("panelRoot").objectReferenceValue = eventPanelGo;
            panelSo.FindProperty("dimmer").objectReferenceValue = dimmer;
            panelSo.FindProperty("eventBg").objectReferenceValue = eventBg;
            panelSo.FindProperty("eventNameText").objectReferenceValue = eventName;
            panelSo.FindProperty("narrativeView").objectReferenceValue = narrative;
            panelSo.FindProperty("attributeView").objectReferenceValue = attrView;
            panelSo.FindProperty("cardSlotPanel").objectReferenceValue = slotPanel;
            panelSo.FindProperty("cardSlotPrefab").objectReferenceValue = slotPrefab;
            panelSo.FindProperty("confirmButton").objectReferenceValue = confirmBtn;
            panelSo.FindProperty("cancelButton").objectReferenceValue = cancelBtn;
            panelSo.FindProperty("handTray").objectReferenceValue = handTray;
            panelSo.ApplyModifiedPropertiesWithoutUndo();

            // Bootstrap
            var boot = bootstrapGo.GetComponent<GamePlayBootstrap>()
                       ?? bootstrapGo.AddComponent<GamePlayBootstrap>();
            var bootSo = new SerializedObject(boot);
            bootSo.FindProperty("handTray").objectReferenceValue = handTray;
            bootSo.FindProperty("startingDeck").objectReferenceValue = deck;
            bootSo.FindProperty("worldCamera").objectReferenceValue = Camera.main;
            bootSo.FindProperty("hideEventPanelOnStart").boolValue = true;
            bootSo.FindProperty("eventPanel").objectReferenceValue = panel;
            bootSo.ApplyModifiedPropertiesWithoutUndo();

            // Map markers + board
            if (markersRoot != null)
            {
                var board = markersRoot.GetComponent<MapEventBoard>()
                            ?? markersRoot.AddComponent<MapEventBoard>();
                var entries = new List<MapEventBoard.Entry>();
                var markers = markersRoot.GetComponentsInChildren<Transform>(true);
                int wired = 0;
                foreach (Transform t in markers)
                {
                    if (t == markersRoot.transform)
                        continue;
                    if (t.parent != markersRoot.transform)
                        continue;

                    var marker = t.GetComponent<MapEventMarker>() ?? t.gameObject.AddComponent<MapEventMarker>();
                    // 先全部挂上示例事件；之后可在 Inspector 换成各自 EventData
                    marker.eventData = sampleEvent;
                    entries.Add(new MapEventBoard.Entry
                    {
                        marker = marker,
                        eventData = sampleEvent,
                        visibleOnStart = wired < 3 // 默认只显示前 3 个，其余可在板子上改
                    });
                    wired++;
                }

                var boardSo = new SerializedObject(board);
                var listProp = boardSo.FindProperty("entries");
                listProp.ClearArray();
                for (int i = 0; i < entries.Count; i++)
                {
                    listProp.InsertArrayElementAtIndex(i);
                    var elem = listProp.GetArrayElementAtIndex(i);
                    elem.FindPropertyRelative("marker").objectReferenceValue = entries[i].marker;
                    elem.FindPropertyRelative("eventData").objectReferenceValue = entries[i].eventData;
                    elem.FindPropertyRelative("visibleOnStart").boolValue = entries[i].visibleOnStart;
                }

                boardSo.ApplyModifiedPropertiesWithoutUndo();
                board.RefreshVisibility();
            }
        }

        static void UpdateSampleEventAttributes()
        {
            var evt = AssetDatabase.LoadAssetAtPath<EventData>(EventAssetPath);
            if (evt == null)
                return;

            evt.requiredAttributes = new ElementRequirement
            {
                metal = 0,
                wood = 2,
                water = 0,
                fire = 0,
                earth = 1
            };
            evt.previewOutcomeText = "若属性足够，你将采得草药与种子；不足则可能空手而归，甚至受伤。";
            EditorUtility.SetDirty(evt);
        }
    }
}
#endif
