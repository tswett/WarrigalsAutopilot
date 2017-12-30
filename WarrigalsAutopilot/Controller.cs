using System;
using UnityEngine;
using WarrigalsAutopilot.ControlElements;
using WarrigalsAutopilot.ControlTargets;

namespace WarrigalsAutopilot
{
    public class Controller
    {
        public ControlTarget Target { get; set; }
        public ControlElement ControlElement { get; set; }
        public float SetPoint { get; set; }
        public float CoeffP { get; set; }
        public float CoeffI { get; set; }
        bool _enabled;
        public bool GuiEnabled { get; set; }
        public float Output { get; private set; }

        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                if (value && !_enabled)
                {
                    _enabled = true;
                    ControlElement.OnEnable();
                }
                else if (!value && _enabled)
                {
                    _enabled = false;
                    ControlElement.OnDisable();
                }
            }
        }

        public void Update()
        {
            if (Enabled)
            {
                float error = Target.ErrorFromSetPoint(SetPoint);
                ControlElement.Trim += CoeffI * -error * Time.fixedDeltaTime;
                Output = CoeffP * -error + ControlElement.Trim;

                if (Output < ControlElement.MinOutput)
                {
                    // set the trim to the lowest feasible value
                    Output = ControlElement.MinOutput;
                    ControlElement.Trim = Output + CoeffP * error;
                }
                else if (Output > ControlElement.MaxOutput)
                {
                    // set the trim to the highest feasible value
                    Output = ControlElement.MaxOutput;
                    ControlElement.Trim = Output + CoeffP * error;
                }

                ControlElement.SetOutput(Output);
            }
        }

        public void PaintGui(int windowId)
        {
            if (GuiEnabled)
            {
                GUILayout.Window(
                    id: windowId,
                    screenRect: new Rect(100, 300, 500, 200),
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
            GUILayout.Label($"Set point: {SetPoint}", GUILayout.Width(200));
            SetPoint = GUILayout.HorizontalSlider(
                SetPoint, Target.MinSetPoint, Target.MaxSetPoint, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.Label($"{Target.Name}: {Target.ProcessVariable}");

            GUILayout.BeginHorizontal();
            GUILayout.Label($"Trim: {ControlElement.Trim}", GUILayout.Width(200));
            ControlElement.Trim = GUILayout.HorizontalSlider(
                ControlElement.Trim, ControlElement.MinOutput, ControlElement.MaxOutput, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"P coefficient: {CoeffP}", GUILayout.Width(200));
            CoeffP = GUILayout.HorizontalSlider(CoeffP, 0.0f, 0.05f, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"I coefficient: {CoeffI}", GUILayout.Width(200));
            CoeffI = GUILayout.HorizontalSlider(CoeffI, 0.0f, 0.003f, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }
    }
}
