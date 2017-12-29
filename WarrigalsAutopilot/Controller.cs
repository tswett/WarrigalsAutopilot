using System;
using UnityEngine;

namespace WarrigalsAutopilot
{
    public class Controller
    {
        public ControlTarget Target { get; set; }
        public float SetPoint { get; set; }
        public float CoeffP { get; set; }
        public float CoeffI { get; set; }
        public bool Enabled { get; set; }
        public bool GuiEnabled { get; set; }
        public event OutputReceiver OnOutput = delegate { };

        float _errorIntegral;

        public delegate void OutputReceiver(float output);

        public float Output
        {
            get
            {
                float error = Target.ErrorFromSetPoint(SetPoint);
                float output = -(CoeffP * error + CoeffI * _errorIntegral);
                //Debug.Log(string.Format(
                //    "WAP: target {0}, value {1}, set point {2}, error {3}, output {4}",
                //    Target.Name, Target.ProcessVariable, SetPoint, error, output));
                return output;
            }
        }

        public void Update()
        {
            if (Enabled)
            {
                _errorIntegral += Target.ErrorFromSetPoint(SetPoint) * Time.fixedDeltaTime;
                OnOutput(Output);
            }
        }

        public void PaintGui(int windowId)
        {
            if (GuiEnabled)
            {
                GUILayout.Window(
                    id: windowId,
                    screenRect: new Rect(400, 100, 500, 200),
                    func: OnWindow,
                    text: Target.Name);
            }
        }

        void OnWindow(int id)
        {
            GUILayout.BeginVertical();

            Enabled = GUILayout.Toggle(
                value: Enabled,
                text: Enabled ? "Enabled" : "Disabled",
                style: "button");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"P coefficient: {CoeffP}", GUILayout.Width(200));
            CoeffP = GUILayout.HorizontalSlider(CoeffP, 0.0f, 0.05f, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"I coefficient: {CoeffI}", GUILayout.Width(200));
            CoeffI = GUILayout.HorizontalSlider(CoeffI, 0.0f, 0.003f, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Set point: {SetPoint}", GUILayout.Width(200));
            SetPoint = GUILayout.HorizontalSlider(
                SetPoint, Target.MinSetPoint, Target.MaxSetPoint, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.Label($"{Target.Name}: {Target.ProcessVariable}");

            GUILayout.EndVertical();
        }
    }
}
