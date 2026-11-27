using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using XHHY.Game.Data;
using XHHY.Game.Map;
using XHHY.Game.Runtime;
using XHHY.Game.UI;

namespace XHHY.Game
{
    /// <summary>
    /// 场景启动：补齐点击/相机/手牌。内容数据来自 Inspector 引用。
    /// </summary>
    public class GamePlayBootstrap : MonoBehaviour
    {
        [SerializeField] HandTrayController handTray;
        [SerializeField] DeckConfig startingDeck;
        [SerializeField] Camera worldCamera;
        [SerializeField] bool hideEventPanelOnStart = true;
        [SerializeField] EventPanelController eventPanel;
        [SerializeField] CommonEventRunner commonEventRunner;
        [SerializeField] System.Collections.Generic.List<CommonEventData> commonEvents;

        void Awake()
        {
            GameState.ResetAll();

            if (worldCamera == null)
                worldCamera = Camera.main;

            EnsurePhysics2DRaycaster();
            EnsureUiInputModule();
            EnsureCameraPanZoom();

            if (hideEventPanelOnStart && eventPanel != null)
                eventPanel.Close();

            if (handTray != null)
            {
                handTray.gameObject.SetActive(true);
                handTray.BringToFront();
            }
        }

        void Start()
        {
            var state = GameState.Ensure();
            if (handTray != null && startingDeck != null)
                handTray.BuildFromDeck(startingDeck);

            if (commonEventRunner == null)
                commonEventRunner = GetComponent<CommonEventRunner>();
            if (commonEventRunner == null)
                commonEventRunner = gameObject.AddComponent<CommonEventRunner>();
            commonEventRunner.Bind(state, commonEvents);
        }

        void EnsurePhysics2DRaycaster()
        {
            if (worldCamera == null)
                return;

            if (worldCamera.GetComponent<Physics2DRaycaster>() == null)
                worldCamera.gameObject.AddComponent<Physics2DRaycaster>();
        }

        void EnsureCameraPanZoom()
        {
            if (worldCamera == null)
                return;

            if (worldCamera.GetComponent<CameraPanZoom>() == null)
                worldCamera.gameObject.AddComponent<CameraPanZoom>();
        }

        void EnsureUiInputModule()
        {
            var es = EventSystem.current;
            if (es == null)
                return;

            if (es.GetComponent<InputSystemUIInputModule>() == null
                && es.GetComponent<BaseInputModule>() == null)
            {
                es.gameObject.AddComponent<InputSystemUIInputModule>();
            }
        }
    }
}
