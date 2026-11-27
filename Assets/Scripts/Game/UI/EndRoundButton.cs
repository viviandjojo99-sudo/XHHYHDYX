using UnityEngine;
using UnityEngine.UI;
using XHHY.Game.Runtime;

namespace XHHY.Game.UI
{
    /// <summary>
    /// 可选：挂在「结束回合」按钮上。
    /// </summary>
    public class EndRoundButton : MonoBehaviour
    {
        [SerializeField] Button button;

        void Awake()
        {
            if (button == null)
                button = GetComponent<Button>();
            if (button != null)
            {
                button.onClick.RemoveListener(EndRound);
                button.onClick.AddListener(EndRound);
            }
        }

        public void EndRound()
        {
            GameState.Ensure().AdvanceRound();
        }
    }
}
