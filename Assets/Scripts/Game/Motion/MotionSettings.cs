using UnityEngine;

namespace XHHY.Game.Motion
{
    [CreateAssetMenu(fileName = "MotionSettings", menuName = "炼丹游戏/动效设置", order = 40)]
    public class MotionSettings : ScriptableObject
    {
        [Header("默认弹簧")]
        public float stiffness = 380f;
        public float damping = 28f;
        public int substeps = 3;

        [Header("拖拽中")]
        public float dragStiffness = 1050f;
        public float dragDamping = 54f;

        [Header("倾斜")]
        public float tiltPerVelocity = 0.0052f;
        public float maxTiltDeg = 4f;

        [Header("换位滞回")]
        [Range(0.5f, 0.7f)] public float swapEnter = 0.52f;
        [Range(0.3f, 0.5f)] public float swapExit = 0.48f;

        [Header("面板开合")]
        public float panelFromScale = 0.96f;
        public float panelOpenStiffness = 420f;
        public float panelOpenDamping = 30f;

        [Header("标签滑块")]
        public float tagUnderlineStiffness = 380f;
        public float tagUnderlineDamping = 28f;

        static MotionSettings _runtime;

        public static MotionSettings CreateRuntimeDefaults()
        {
            if (_runtime == null)
                _runtime = CreateInstance<MotionSettings>();
            return _runtime;
        }
    }
}
