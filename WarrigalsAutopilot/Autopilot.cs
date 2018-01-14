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
        Controller _headingController;
        Controller _pitchController;
        Controller _vertSpeedController;
        Controller _altitudeController;
        bool _singleStep = false;

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

            _bankController = new Controller
            {
                Name = "Wing leveler",
                Target = new BankTarget(ActiveVessel),
                ControlElement = new AileronElement(ActiveVessel),
                SetPoint = 0.0f,
                CoeffP = 0.001f,
            };

            _headingController = new Controller
            {
                Name = "Heading hold",
                Target = new HeadingTarget(ActiveVessel),
                ControlElement = new BankElement(_bankController),
                SetPoint = 90.0f,
                CoeffP = 0.5f,
                SliderMaxCoeffP = 2.0f,
                SliderMaxCoeffI = 0.15f,
            };

            _pitchController = new Controller
            {
                Name = "Pitch control",
                Target = new PitchTarget(ActiveVessel),
                ControlElement = new ElevatorElement(ActiveVessel),
                SetPoint = 5.0f,
                CoeffP = 0.01f,
                CoeffI = 0.01f,
            };

            _vertSpeedController = new Controller
            {
                Name = "Vertical speed",
                Target = new VertSpeedTarget(ActiveVessel),
                ControlElement = new PitchElement(_pitchController),
                SetPoint = 0.0f,
                CoeffP = 1.0f,
                CoeffI = 0.1f,
                SliderMaxCoeffP = 10.0f,
                SliderMaxCoeffI = 1.0f,
            };

            _altitudeController = new Controller
            {
                Name = "Altitude hold",
                Target = new AltitudeTarget(ActiveVessel),
                ControlElement = new VertSpeedElement(_vertSpeedController),
                SetPoint = 2000.0f,
                CoeffP = 0.5f,
                SliderMaxCoeffP = 1.0f,
            };

            Debug.Log("WAP: End Autopilot.Start");
        }

        public void OnDisable()
        {
            ApplicationLauncher.Instance.RemoveModApplication(_appLauncherButton);
        }

        void FixedUpdate()
        {
            _headingController.Update();
            _bankController.Update();

            _altitudeController.Update();
            _vertSpeedController.Update();
            _pitchController.Update();

            if (_singleStep) FlightDriver.SetPause(true, postScreenMessage: false);
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

                _bankController.PaintDetailGui(windowId: 1);
                _headingController.PaintDetailGui(windowId: 2);
                _pitchController.PaintDetailGui(windowId: 3);
                _vertSpeedController.PaintDetailGui(windowId: 4);
                _altitudeController.PaintDetailGui(windowId: 5);
            }
        }

        void OnWindow(int id)
        {
            GUILayout.BeginVertical();

            _bankController.PaintSmallGui();
            _headingController.PaintSmallGui();
            _pitchController.PaintSmallGui();
            _vertSpeedController.PaintSmallGui();
            _altitudeController.PaintSmallGui();

#if DEBUG
            GUILayout.BeginHorizontal();
            _singleStep = GUILayout.Toggle(_singleStep, "DEBUG: Single step");
            DebugLogger.Verbose = _singleStep;
            if (GUILayout.Button("Go")) FlightDriver.SetPause(false, postScreenMessage: false);
            GUILayout.EndHorizontal();
#endif

            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 1000, 1000));
        }
    }
}
