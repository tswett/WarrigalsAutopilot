using System;

namespace WarrigalsAutopilot
{
    public abstract class ControlTarget
    {
        public abstract string Name { get; }

        public abstract float ProcessVariable { get; }
        public abstract float MinSetPoint { get; }
        public abstract float MaxSetPoint { get; }

        public virtual float ErrorFromSetPoint(float setPoint)
        {
            return ProcessVariable - setPoint;
        }

        internal static float AngleSubtract(float angle, float minusAngle)
        {
            float result = (angle - minusAngle) % 360.0f;

            if (result <= -180) result += 360;
            if (result > 180) result -= 360;

            return result;
        }
    }
}
