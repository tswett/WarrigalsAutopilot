// Copyright 2018 by Tanner "Warrigal" Swett.

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
    /// <summary>
    /// A PID controller, used to indirectly control some target value by
    /// directly manipulating some control element.
    /// </summary>
    public class Controller
    {
        /// <summary>
        /// The name of this controller, such as "Heading hold".
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// The target value which this controller attempts to indirectly control.
        /// </summary>
        public Target Target { get; set; }
        /// <summary>
        /// The control element which this controller directly manipulates in order to indirectly
        /// control some target value.
        /// </summary>
        public Element ControlElement { get; set; }
        public float CoeffP { get; set; }
        public float CoeffI { get; set; }
        public float SliderMaxCoeffP { get; set; } = 0.5f;
        public float SliderMaxCoeffI { get; set; } = 0.03f;
        public bool Enabled { get; set; }
        public bool GuiEnabled { get; set; }
        public float Output { get; private set; }

        float _setPoint;
        Rect _windowRectangle = new Rect(100, 300, 500, 200);
        float? _minOutput;
        float? _maxOutput;

        public float SetPoint
        {
            get => _setPoint;
            set => _setPoint = Math.Max(Target.MinSetPoint, Math.Min(Target.MaxSetPoint, value));
        }

        public float MinOutput
        {
            get => _minOutput ?? ControlElement.MinOutput;
            set => _minOutput = value;
        }

        public float MaxOutput
        {
            get => _maxOutput ?? ControlElement.MaxOutput;
            set => _maxOutput = value;
        }

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
            DebugLogger.LogVerbose("Controller name: " + Name);

            if (Enabled)
            {
                DebugLogger.LogVerbose($"Old trim: {ControlElement.Trim}");

                float error = Target.ErrorFromSetPoint(SetPoint);
                ControlElement.Trim += CoeffI * -error * Time.fixedDeltaTime;
                Output = CoeffP * -error + ControlElement.Trim;

                DebugLogger.LogVerbose($"Error: {error}");
                DebugLogger.LogVerbose($"New trim: {ControlElement.Trim}");
                DebugLogger.LogVerbose($"Output: {Output}");

                if (Output < MinOutput)
                {
                    Output = MinOutput;

                    if (ControlElement.Trim < 0)
                    {
                        DebugLogger.LogVerbose("Trim too low");

                        // set the trim to the lowest feasible value, but no higher than 0
                        ControlElement.Trim = Math.Min(0.0f, Output + CoeffP * error);

                        DebugLogger.LogVerbose($"New trim: {ControlElement.Trim}");
                        DebugLogger.LogVerbose($"Output: {Output}");
                    }
                }
                else if (Output > MaxOutput)
                {
                    Output = MaxOutput;

                    if (ControlElement.Trim > 0)
                    {
                        DebugLogger.LogVerbose("Trim too high");

                        // set the trim to the highest feasible value, but no lower than 0
                        Output = MaxOutput;
                        ControlElement.Trim = Math.Max(0.0f, Output + CoeffP * error);

                        DebugLogger.LogVerbose($"New trim: {ControlElement.Trim}");
                        DebugLogger.LogVerbose($"Output: {Output}");
                    }
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
                options: new[] { GUILayout.Width(150.0f) });

            int setPointInt = Mathf.RoundToInt(SetPoint);

            int newSetPointInt = Odospinner.Paint(
                setPointInt,
                minValue: Target.MinSetPointInt,
                maxValue: Target.MaxSetPointInt,
                wrapAround: Target.WrapAround);

            if (newSetPointInt != setPointInt)
            {
                SetPoint = newSetPointInt;
            }

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
