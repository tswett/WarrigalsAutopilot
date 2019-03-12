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
using WarrigalsAutopilot.Controllers;
using WarrigalsAutopilot.ControlTargets;

namespace WarrigalsAutopilot
{
    [KSPAddon(startup: KSPAddon.Startup.Flight, once: false)]
    public class Autopilot : MonoBehaviour
    {
        bool _showGui = false;
        ApplicationLauncherButton _appLauncherButton;
        Rect _windowRectangle = new Rect(100, 100, 400, 100);
        BankController _bankController;
        PidController _headingController;
        PitchController _pitchController;
        VertSpeedController _vertSpeedController;
        PidController _altitudeController;
        PidController _speedByPitchController;
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

            _bankController = new BankController(ActiveVessel);
            _bankController.OnDisable += () => _headingController.Enabled = false;

            _headingController = new HeadingController(ActiveVessel, _bankController);
            _headingController.OnEnable += () => _bankController.Enabled = true;

            _pitchController = new PitchController(ActiveVessel);
            _pitchController.OnDisable += () =>
            {
                _vertSpeedController.Enabled = false;
                _speedByPitchController.Enabled = false;
            };

            _vertSpeedController = new VertSpeedController(ActiveVessel, _pitchController);
            _vertSpeedController.OnEnable += () =>
            {
                _pitchController.Enabled = true;
                _speedByPitchController.Enabled = false;
            };
            _vertSpeedController.OnDisable += () =>
            {
                _altitudeController.Enabled = false;
            };

            _altitudeController = new AltitudeController(ActiveVessel, _vertSpeedController);
            _altitudeController.OnEnable += () =>
            {
                _vertSpeedController.Enabled = true;
            };

            _speedByPitchController = new SpeedByPitchController(ActiveVessel, _pitchController);
            _speedByPitchController.OnEnable += () =>
            {
                _pitchController.Enabled = true;
                _vertSpeedController.Enabled = false;
            };

            Debug.Log("WAP: End Autopilot.Start");
        }

        public void OnDisable()
        {
            ApplicationLauncher.Instance.RemoveModApplication(_appLauncherButton);
        }

        bool _abortFixedUpdate = false;
        void FixedUpdate()
        {
            if (_abortFixedUpdate) return;

            try
            {
                _headingController.Update();
                _bankController.Update();

                _altitudeController.Update();
                _vertSpeedController.Update();
                _speedByPitchController.Update();
                _pitchController.Update();

                if (_singleStep) FlightDriver.SetPause(true, postScreenMessage: false);
            }
            catch
            {
                Debug.Log("WAP: Exception occurred in FixedUpdate; stopping.");
                _abortFixedUpdate = true;
                throw;
            }
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
                _speedByPitchController.PaintDetailGui(windowId: 6);
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
            _speedByPitchController.PaintSmallGui();

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
