using TMPro;
using UnityEngine;
using XHHY.Game.Data;

namespace XHHY.Game.UI
{
    /// <summary>
    /// 显示事件需求属性 vs 已填入卡牌属性合计，并给出是否占优。
    /// </summary>
    public class EventAttributeView : MonoBehaviour
    {
        [SerializeField] TMP_Text summaryText;

        public void BindText(TMP_Text text)
        {
            summaryText = text;
        }

        public void Show(ElementRequirement required, ElementRequirement provided)
        {
            if (summaryText == null)
                return;

            required ??= new ElementRequirement();
            provided ??= new ElementRequirement();

            int adv =
                (provided.metal - required.metal) +
                (provided.wood - required.wood) +
                (provided.water - required.water) +
                (provided.fire - required.fire) +
                (provided.earth - required.earth);

            bool ok = adv >= 0;
            summaryText.text =
                $"需求 金{required.metal} 木{required.wood} 水{required.water} 火{required.fire} 土{required.earth}\n" +
                $"当前 金{provided.metal} 木{provided.wood} 水{provided.water} 火{provided.fire} 土{provided.earth}\n" +
                $"优势值 {adv}  {(ok ? "可尝试" : "不足")}";
        }

        public void Clear()
        {
            if (summaryText != null)
                summaryText.text = string.Empty;
        }
    }
}
