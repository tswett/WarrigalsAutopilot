using System;

namespace WarrigalsAutopilot.ControlElements
{
    public abstract class ControlElement
    {
        public float Trim { get; set; }
        public abstract float MinOutput { get; }
        public abstract float MaxOutput { get; }
        public abstract void SetOutput(float output);
        public virtual void OnEnable() { }
        public virtual void OnDisable() { }
    }
}
