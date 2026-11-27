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
using XHHY.Game.Motion;
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

            // 数量角标
            var countTf = root.transform.Find("CountBadge");
            if (countTf == null)
            {
                var countGo = new GameObject("CountBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
                countGo.transform.SetParent(root.transform, false);
                var crt = countGo.GetComponent<RectTransform>();
                crt.anchorMin = new Vector2(1f, 1f);
                crt.anchorMax = new Vector2(1f, 1f);
                crt.pivot = new Vector2(1f, 1f);
                crt.anchoredPosition = new Vector2(-8f, -8f);
                crt.sizeDelta = new Vector2(48f, 28f);
                var ctmp = countGo.GetComponent<TextMeshProUGUI>();
                ctmp.fontSize = 20;
                ctmp.alignment = TextAlignmentOptions.TopRight;
                ctmp.color = Color.white;
                countGo.SetActive(false);
                countTf = countGo.transform;
            }

            // 属性小块行
            var attrTf = root.transform.Find("AttrRow");
            if (attrTf == null)
            {
                var rowGo = new GameObject("AttrRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                rowGo.transform.SetParent(root.transform, false);
                var rrt = rowGo.GetComponent<RectTransform>();
                rrt.anchorMin = new Vector2(0f, 0f);
                rrt.anchorMax = new Vector2(1f, 0f);
                rrt.pivot = new Vector2(0.5f, 0f);
                rrt.anchoredPosition = new Vector2(0f, 8f);
                rrt.sizeDelta = new Vector2(-12f, 28f);
                var h = rowGo.GetComponent<HorizontalLayoutGroup>();
                h.childAlignment = TextAnchor.MiddleCenter;
                h.spacing = 4f;
                h.childForceExpandWidth = false;
                h.childForceExpandHeight = false;
                for (int i = 0; i < 4; i++)
                {
                    var chip = new GameObject("Chip" + i, typeof(RectTransform), typeof(Image), typeof(TextMeshProUGUI));
                    chip.transform.SetParent(rowGo.transform, false);
                    var chipRt = chip.GetComponent<RectTransform>();
                    chipRt.sizeDelta = new Vector2(36f, 24f);
                    var img = chip.GetComponent<Image>();
                    img.color = new Color(0f, 0f, 0f, 0.35f);
                    img.raycastTarget = false;
                    var tmp = chip.GetComponent<TextMeshProUGUI>();
                    tmp.fontSize = 14;
                    tmp.alignment = TextAlignmentOptions.Center;
                    tmp.color = Color.white;
                    tmp.raycastTarget = false;
                }
                attrTf = rowGo.transform;
            }

            var typeIconTf = root.transform.Find("TypeIcon")?.GetComponent<Image>();
            var equipDotTf = root.transform.Find("EquipDot")?.GetComponent<Image>();

            var so = new SerializedObject(view);
            so.FindProperty("nameText").objectReferenceValue = name;
            so.FindProperty("iconImage").objectReferenceValue = icon;
            var countProp = so.FindProperty("countText");
            if (countProp != null)
                countProp.objectReferenceValue = countTf.GetComponent<TMP_Text>();
            var attrProp = so.FindProperty("attributeRow");
            if (attrProp != null)
                attrProp.objectReferenceValue = attrTf as RectTransform;
            var typeProp = so.FindProperty("typeIcon");
            if (typeProp != null)
                typeProp.objectReferenceValue = typeIconTf;
            var equipProp = so.FindProperty("equipDot");
            if (equipProp != null)
                equipProp.objectReferenceValue = equipDotTf;
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
            var typeCatalog = AssetDatabase.LoadAssetAtPath<CardTypeCatalog>(CatalogPath);
            var sampleEvent = AssetDatabase.LoadAssetAtPath<EventData>(EventAssetPath);

            // HandTray
            var handTray = handTrayGo.GetComponent<HandTrayController>() ?? handTrayGo.AddComponent<HandTrayController>();
            var handTraySo = new SerializedObject(handTray);
            handTraySo.FindProperty("handCardContainer").objectReferenceValue = handCardGo.transform;
            handTraySo.FindProperty("cardPrefab").objectReferenceValue = cardPrefab;
            handTraySo.FindProperty("deckConfig").objectReferenceValue = deck;
            var layout = AssetDatabase.LoadAssetAtPath<HandLayoutSettings>("Assets/Data/HandLayoutSettings.asset");
            var layoutProp = handTraySo.FindProperty("layoutSettings");
            if (layoutProp != null)
                layoutProp.objectReferenceValue = layout;
            var motion = AssetDatabase.LoadAssetAtPath<MotionSettings>("Assets/Data/MotionSettings.asset");
            var motionProp = handTraySo.FindProperty("motionSettings");
            if (motionProp != null)
                motionProp.objectReferenceValue = motion;
            var sfx = AssetDatabase.LoadAssetAtPath<UiSfx>("Assets/Data/UiSfx.asset");
            var sfxProp = handTraySo.FindProperty("uiSfx");
            if (sfxProp != null)
                sfxProp.objectReferenceValue = sfx;
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
            tagSo.FindProperty("catalog").objectReferenceValue = typeCatalog;
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
                var tierProp = attrSo.FindProperty("tierText");
                if (tierProp != null)
                    tierProp.objectReferenceValue = attrText;
                var catalog = AssetDatabase.LoadAssetAtPath<AttributeCatalog>("Assets/Data/Attributes/AttributeCatalog.asset");
                var catProp = attrSo.FindProperty("catalog");
                if (catProp != null)
                    catProp.objectReferenceValue = catalog;
                var rowPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefab/Attribute.prefab");
                var rowProp = attrSo.FindProperty("rowPrefab");
                if (rowProp != null)
                    rowProp.objectReferenceValue = rowPrefab;
                var rowContainerProp = attrSo.FindProperty("rowContainer");
                if (rowContainerProp != null)
                    rowContainerProp.objectReferenceValue = attributePanel as RectTransform;
                var motionAttr = AssetDatabase.LoadAssetAtPath<MotionSettings>("Assets/Data/MotionSettings.asset");
                var motionAttrProp = attrSo.FindProperty("motionSettings");
                if (motionAttrProp != null)
                    motionAttrProp.objectReferenceValue = motionAttr;
                attrSo.ApplyModifiedPropertiesWithoutUndo();
            }

            // CardInformation 详情面板
            var cardInfoGo = GameObject.Find("CardInformation");
            if (cardInfoGo != null)
            {
                var info = cardInfoGo.GetComponent<CardInformationPanel>()
                           ?? cardInfoGo.AddComponent<CardInformationPanel>();
                var infoSo = new SerializedObject(info);
                infoSo.FindProperty("root").objectReferenceValue = cardInfoGo;
                infoSo.FindProperty("handTray").objectReferenceValue = handTray;
                infoSo.FindProperty("equipSlotPrefab").objectReferenceValue = slotPrefab;
                infoSo.ApplyModifiedPropertiesWithoutUndo();
                cardInfoGo.SetActive(false);
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
            var panelMotion = AssetDatabase.LoadAssetAtPath<MotionSettings>("Assets/Data/MotionSettings.asset");
            var panelMotionProp = panelSo.FindProperty("motionSettings");
            if (panelMotionProp != null)
                panelMotionProp.objectReferenceValue = panelMotion;
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
