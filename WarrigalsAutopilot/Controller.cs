using System;

namespace WarrigalsAutopilot
{
    class Controller
    {
        public ControlTarget Target { get; set; }
        public float SetPoint { get; set; }
        public float CoeffP { get; set; }

        public float Output
        {
            get
            {
                float error = Target.ErrorFromSetPoint(SetPoint);
                float output = -(CoeffP * error);
                //Debug.Log(string.Format(
                //    "WAP: target {0}, value {1}, set point {2}, error {3}, output {4}",
                //    Target.Name, Target.ProcessVariable, SetPoint, error, output));
                return output;
            }
        }
    }
}
