using UnityEngine;
using UnityEngine.UI;

namespace XHHY.Game.UI
{
    /// <summary>
    /// 可选：挂在关闭按钮上，点击关闭 EventPanel。
    /// </summary>
    [RequireComponent(typeof(Button))]
    public class EventCloseButton : MonoBehaviour
    {
        void Awake()
        {
            GetComponent<Button>().onClick.AddListener(() =>
            {
                if (EventPanelController.Instance != null)
                    EventPanelController.Instance.Close();
            });
        }
    }
}
