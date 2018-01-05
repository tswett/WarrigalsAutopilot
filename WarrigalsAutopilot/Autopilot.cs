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

using KSP.UI.Screens;
using System;
using UnityEngine;
using WarrigalsAutopilot.ControlElements;
using WarrigalsAutopilot.ControlTargets;

namespace WarrigalsAutopilot
{
    [KSPAddon(startup: KSPAddon.Startup.Flight, once: false)]
    public class Autopilot : MonoBehaviour
    {
        bool _showGui = false;
        ApplicationLauncherButton _appLauncherButton;
        Rect _windowRectangle = new Rect(100, 100, 400, 100);
        Controller _bankController;
        Controller _pitchController;

        Vessel ActiveVessel => FlightGlobals.ActiveVessel;

        public void Start()
        {
            Debug.Log("WAP: Begin Autopilot.Start");

            Texture launcherButtonTexture =
                GameDatabase.Instance.GetTexture("WarrigalsAutopilot/wap-icon", asNormalMap: false);
            _appLauncherButton = ApplicationLauncher.Instance.AddModApplication(
                onTrue: () => _showGui = true,
                onFalse: () => _showGui = false,
                onHover: null,
                onHoverOut: null,
                onEnable: null,
                onDisable: null,
                visibleInScenes: ApplicationLauncher.AppScenes.FLIGHT,
                texture: launcherButtonTexture
                );

            Debug.Log("WAP: Creating _bankController");
            _bankController = new Controller
            {
                Target = new BankControlTarget(ActiveVessel),
                ControlElement = new AileronControlElement(ActiveVessel),
                SetPoint = 0.0f,
                CoeffP = 0.001f,
            };

            Debug.Log("WAP: Creating _pitchController");
            _pitchController = new Controller
            {
                Target = new PitchControlTarget(ActiveVessel),
                ControlElement = new ElevatorControlElement(ActiveVessel),
                SetPoint = 5.0f,
                CoeffP = 0.01f,
                CoeffI = 0.0005f,
            };

            Debug.Log(
                $"WAP: End Autopilot.Start, _bankController is {_bankController}, " +
                $"_pitchController is {_pitchController}");
        }

        public void OnDisable()
        {
            ApplicationLauncher.Instance.RemoveModApplication(_appLauncherButton);
        }

        void FixedUpdate()
        {
            _bankController.Update();
            _pitchController.Update();
        }

        void OnGUI()
        {
            if (_showGui)
            {
                _windowRectangle = GUILayout.Window(
                    id: 0,
                    screenRect: _windowRectangle,
                    func: OnWindow,
                    text: "Warrigal's Autopilot");
            }

            _bankController.PaintGui(windowId: 1);
            _pitchController.PaintGui(windowId: 2);
        }

        void OnWindow(int id)
        {
            GUILayout.BeginVertical();

            PaintController(_bankController, "Wing leveler");
            PaintController(_pitchController, "Pitch control");

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        void PaintController(Controller controller, string text)
        {
            GUILayout.BeginHorizontal();

            controller.Enabled = GUILayout.Toggle(
                value: controller.Enabled,
                text: text,
                style: "button");

            controller.SetPoint = GUILayout.HorizontalSlider(
                value: controller.SetPoint,
                leftValue: controller.Target.MinSetPoint,
                rightValue: controller.Target.MaxSetPoint,
                options: new[] { GUILayout.Width(200) });

            controller.GuiEnabled = GUILayout.Toggle(
                value: controller.GuiEnabled,
                text: "GUI",
                style: Styles.SafeButton);

            GUILayout.EndHorizontal();
        }
    }
}
