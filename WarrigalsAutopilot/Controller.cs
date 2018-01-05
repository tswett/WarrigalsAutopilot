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
        public ControlTarget Target { get; set; }
        public ControlElement ControlElement { get; set; }
        public float SetPoint { get; set; }
        public float CoeffP { get; set; }
        public float CoeffI { get; set; }
        public bool Enabled { get; set; }
        public bool GuiEnabled { get; set; }
        public float Output { get; private set; }

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
