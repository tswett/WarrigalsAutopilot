﻿using KSP.UI.Screens;
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
                GUILayout.Window(
                    id: 0,
                    screenRect: new Rect(100, 100, 400, 100),
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
