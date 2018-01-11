// Copyright 2017 by Tanner "Warrigal" Swett.

// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.

// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.

// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;
using UnityEngine;
using WarrigalsAutopilot.ControlElements;
using WarrigalsAutopilot.ControlTargets;

namespace WarrigalsAutopilot
{
    public class Controller
    {
        public string Name { get; set; }
        public ControlTarget Target { get; set; }
        public ControlElement ControlElement { get; set; }
        public float SetPoint { get; set; }
        public float CoeffP { get; set; }
        public float CoeffI { get; set; }
        public float SliderMaxCoeffP { get; set; } = 0.5f;
        public float SliderMaxCoeffI { get; set; } = 0.03f;
        public bool Enabled { get; set; }
        public bool GuiEnabled { get; set; }
        public float Output { get; private set; }

        Rect _windowRectangle = new Rect(100, 300, 500, 200);
        float CoeffPSliderPos
        {
            get => Mathf.Pow(CoeffP / SliderMaxCoeffP, 1.0f / 4.0f);
            set { if (CoeffPSliderPos != value) CoeffP = SliderMaxCoeffP * Mathf.Pow(value, 4.0f); }
        }

        float CoeffISliderPos
        {
            get => Mathf.Pow(CoeffI / SliderMaxCoeffP, 1.0f / 4.0f);
            set { if (CoeffISliderPos != value) CoeffI = SliderMaxCoeffP * Mathf.Pow(value, 4.0f); }
        }

        public void Update()
        {
            if (Enabled)
            {
                float error = Target.ErrorFromSetPoint(SetPoint);
                ControlElement.Trim += CoeffI * -error * Time.fixedDeltaTime;
                Output = CoeffP * -error + ControlElement.Trim;

                if (Output < ControlElement.MinOutput && ControlElement.Trim < 0)
                {
                    // set the trim to the lowest feasible value
                    Output = ControlElement.MinOutput;
                    ControlElement.Trim = Output + CoeffP * error;
                }
                else if (Output > ControlElement.MaxOutput && ControlElement.Trim > 0)
                {
                    // set the trim to the highest feasible value
                    Output = ControlElement.MaxOutput;
                    ControlElement.Trim = Output + CoeffP * error;
                }

                ControlElement.SetOutput(Output);
            }
        }

        public void PaintSmallGui()
        {
            GUILayout.BeginHorizontal();

            Enabled = GUILayout.Toggle(
                value: Enabled,
                text: Name,
                style: "button");

            GUILayout.BeginVertical();

            int setPointInt = Mathf.RoundToInt(SetPoint);
            int newSetPointInt = Odospinner.Paint(
                setPointInt,
                minValue: Mathf.FloorToInt(Target.MinSetPoint),
                maxValue: Mathf.CeilToInt(Target.MaxSetPoint));
            if (newSetPointInt != setPointInt)
            {
                SetPoint = newSetPointInt;
            }

            SetPoint = GUILayout.HorizontalSlider(
                value: SetPoint,
                leftValue: Target.MinSetPoint,
                rightValue: Target.MaxSetPoint,
                options: new[] { GUILayout.Width(200) });

            GUILayout.EndVertical();

            GuiEnabled = GUILayout.Toggle(
                value: GuiEnabled,
                text: "GUI",
                style: Styles.SafeButton);

            GUILayout.EndHorizontal();
        }

        public void PaintDetailGui(int windowId)
        {
            if (GuiEnabled)
            {
                _windowRectangle = GUILayout.Window(
                    id: windowId,
                    screenRect: _windowRectangle,
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
            CoeffPSliderPos = GUILayout.HorizontalSlider(CoeffPSliderPos, 0.0f, 1.0f, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label($"I coefficient: {CoeffI}", GUILayout.Width(200));
            CoeffISliderPos = GUILayout.HorizontalSlider(CoeffISliderPos, 0.0f, 1.0f, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow();
        }
    }
}
