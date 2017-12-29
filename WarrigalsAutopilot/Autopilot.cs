using KSP.UI.Screens;
using System;
using UnityEngine;

namespace WarrigalsAutopilot
{
    [KSPAddon(startup: KSPAddon.Startup.Flight, once: false)]
    public class Autopilot : MonoBehaviour
    {
        bool _showGui = false;
        ApplicationLauncherButton _appLauncherButton;
        Controller _bankController;
        Controller _pitchController;

        Vessel ActiveVessel => FlightGlobals.ActiveVessel;

        public void Start()
        {
            Debug.Log("WAP: Begin Autopilot.Start");
            
            _appLauncherButton = ApplicationLauncher.Instance.AddModApplication(
                onTrue: () => _showGui = true,
                onFalse: () => _showGui = false,
                onHover: null,
                onHoverOut: null,
                onEnable: null,
                onDisable: null,
                visibleInScenes: ApplicationLauncher.AppScenes.FLIGHT,
                texture: new Texture()
                );

            Debug.Log("WAP: Creating _bankController");
            _bankController = new Controller {
                Target = new BankControlTarget(ActiveVessel),
                SetPoint = 0.0f,
                CoeffP = 0.001f,
            };
            _bankController.OnOutput += (output) => FlightGlobals.ActiveVessel.ctrlState.roll = output;

            Debug.Log("WAP: Creating _pitchController");
            _pitchController = new Controller {
                Target = new PitchControlTarget(ActiveVessel),
                SetPoint = 35.0f,
                CoeffP = 0.01f,
                CoeffI = 0.0005f,
            };
            _pitchController.OnOutput += (output) => FlightGlobals.ActiveVessel.ctrlState.pitch = output;

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
                GUILayout.Window(
                    id: 0,
                    screenRect: new Rect(100, 100, 200, 100),
                    func: OnWindow,
                    text: "Warrigal's Autopilot",
                    options: new[] { GUILayout.MinWidth(100) });
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

            controller.GuiEnabled = GUILayout.Toggle(
                value: controller.GuiEnabled,
                text: "GUI",
                style: "button");

            GUILayout.EndHorizontal();
        }
    }
}
