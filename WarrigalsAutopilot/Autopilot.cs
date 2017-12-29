using KSP.UI.Screens;
using System;
using UnityEngine;

namespace WarrigalsAutopilot
{
    [KSPAddon(startup: KSPAddon.Startup.Flight, once: false)]
    public class Autopilot : MonoBehaviour
    {
        bool _wingLevelEnabled = false;
        bool _pitchControlEnabled = false;
        bool _showGui = false;
        ApplicationLauncherButton _appLauncherButton;
        Controller _bankController;
        Controller _pitchController;

        Vessel ActiveVessel => FlightGlobals.ActiveVessel;

        public void Start()
        {
            Debug.Log("WAP: calling AddModApplication...");
            _appLauncherButton = ApplicationLauncher.Instance.AddModApplication(
                onTrue: ShowGui,
                onFalse: HideGui,
                onHover: null,
                onHoverOut: null,
                onEnable: null,
                onDisable: null,
                visibleInScenes: ApplicationLauncher.AppScenes.FLIGHT,
                texture: new Texture()
                );
            Debug.Log("WAP: done calling AddModApplication.");

            ControlTarget bankTarget = new BankControlTarget(ActiveVessel);
            _bankController = new Controller { Target = bankTarget, SetPoint = 0.0f, CoeffP = 0.001f };

            ControlTarget pitchTarget = new PitchControlTarget(ActiveVessel);
            _pitchController = new Controller { Target = pitchTarget, SetPoint = 35.0f, CoeffP = 0.001f };
        }

        public void OnDisable()
        {
            ApplicationLauncher.Instance.RemoveModApplication(_appLauncherButton);
        }

        void ShowGui()
        {
            _showGui = true;
        }

        void HideGui()
        {
            _showGui = false;
        }

        void OnGUI()
        {
            if (_showGui)
            {
                GUILayout.Window(
                    id: 0,
                    screenRect: new Rect(100, 100, 100, 100),
                    func: OnWindow,
                    text: "Warrigal's Autopilot",
                    options: new[] { GUILayout.MinWidth(100) });
            }
        }

        void OnWindow(int id)
        {
            GUILayout.BeginVertical();

            _wingLevelEnabled = GUILayout.Toggle(value: _wingLevelEnabled, text: "Wing leveler");
            _pitchControlEnabled = GUILayout.Toggle(value: _pitchControlEnabled, text: "Pitch control");

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        public void FixedUpdate()
        {
            if (_wingLevelEnabled)
            {
                FlightGlobals.ActiveVessel.ctrlState.roll = _bankController.Output;
            }

            if (_pitchControlEnabled)
            {
                FlightGlobals.ActiveVessel.ctrlState.pitch = _pitchController.Output;
            }
        }
    }
}
