using UnityEngine;

namespace XHHY.Game.Motion
{
    /// <summary>
    /// 二阶弹簧：x'' = -k*(x-target) - c*x'，每帧拆成若干子步。
    /// </summary>
    public struct Spring1D
    {
        public float value;
        public float velocity;

        public void Snap(float target)
        {
            value = target;
            velocity = 0f;
        }

        public void Step(float target, float stiffness, float damping, float dt, int substeps)
        {
            if (float.IsNaN(target) || float.IsInfinity(target))
                return;
            if (stiffness < 0.01f)
            {
                Snap(target);
                return;
            }

            // 编辑器卡顿时 unscaledDeltaTime 会很大，二阶积分会爆掉，牌和面板会被甩出屏幕。
            dt = Mathf.Clamp(dt, 0f, 0.033f);
            if (substeps < 1) substeps = 1;
            float maxH = 0.45f / Mathf.Sqrt(stiffness);
            float h = Mathf.Min(dt / substeps, maxH);
            if (h <= 0f)
                return;

            int steps = Mathf.Max(1, Mathf.CeilToInt(dt / h));
            if (steps > 8) steps = 8;
            for (int i = 0; i < steps; i++)
            {
                float force = -stiffness * (value - target) - damping * velocity;
                velocity += force * h;
                value += velocity * h;
            }

            if (float.IsNaN(value) || float.IsInfinity(value) || float.IsNaN(velocity) || float.IsInfinity(velocity))
                Snap(target);
        }

        public bool NearlySettled(float target, float posEps = 0.05f, float velEps = 1f)
        {
            return Mathf.Abs(value - target) <= posEps && Mathf.Abs(velocity) <= velEps;
        }
    }

    public struct Spring2D
    {
        public Spring1D x;
        public Spring1D y;

        public Vector2 Value => new Vector2(x.value, y.value);
        public Vector2 Velocity => new Vector2(x.velocity, y.velocity);

        public void Snap(Vector2 target)
        {
            x.Snap(target.x);
            y.Snap(target.y);
        }

        public void Step(Vector2 target, float stiffness, float damping, float dt, int substeps)
        {
            x.Step(target.x, stiffness, damping, dt, substeps);
            y.Step(target.y, stiffness, damping, dt, substeps);
        }
    }
}
