using KSP.UI.Screens;
using System;
using UnityEngine;

namespace WarrigalsAutopilot
{
    [KSPAddon(startup: KSPAddon.Startup.Flight, once: false)]
    public class Autopilot : MonoBehaviour
    {
        bool _enabled = false;
        bool _showGui = false;
        ApplicationLauncherButton _appLauncherButton;
        Controller _bankController;

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
            _bankController = new Controller(bankTarget, setPoint: 0.0f);
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

            bool enableWingLevel = GUILayout.Button("Enable wing leveler");
            if (enableWingLevel) _enabled = true;

            bool disableWingLevel = GUILayout.Button("Disable wing leveler");
            if (disableWingLevel) _enabled = false;

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        public void FixedUpdate()
        {
            if (_enabled)
            {
                FlightGlobals.ActiveVessel.ctrlState.roll = _bankController.Output;
            }
        }
    }
}
