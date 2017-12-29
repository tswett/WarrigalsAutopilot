using System;
using UnityEngine;

namespace WarrigalsAutopilot
{
    public class Controller
    {
        public ControlTarget Target { get; set; }
        public float SetPoint { get; set; }
        public float CoeffP { get; set; }
        public bool Enabled { get; set; }
        public bool GuiEnabled { get; set; }
        public event OutputReceiver OnOutput = delegate { };

        public delegate void OutputReceiver(float output);

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

        public void Update()
        {
            if (Enabled) OnOutput(Output);
        }

        public void PaintGui(int windowId)
        {
            if (GuiEnabled)
            {
                GUILayout.Window(
                    id: windowId,
                    screenRect: new Rect(400, 100, 200, 100),
                    func: OnWindow,
                    text: Target.Name);
            }
        }

        void OnWindow(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.Label($"P coefficient: {CoeffP}");
            GUILayout.Label($"Set point: {SetPoint}");
            GUILayout.Label($"Proc variable: {Target.ProcessVariable}");

            GUILayout.EndVertical();
        }
    }
}
