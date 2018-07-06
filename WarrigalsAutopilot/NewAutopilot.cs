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

using AtmosphereAutopilot;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace WarrigalsAutopilot
{
    public sealed class NewAutopilot : StateController
    {
        internal NewAutopilot(Vessel v)
            : base(v, "Warrigal's Autopilot", 478927147)
        { }

        LateralNavigationController latNavController;
        CruiseController cruiseController;

        public override void InitializeDependencies(Dictionary<Type, AutopilotModule> modules)
        {
            latNavController = modules[typeof(LateralNavigationController)] as LateralNavigationController;
            cruiseController = modules[typeof(CruiseController)] as CruiseController;
        }

        protected override void _drawGUI(int id)
        {
            close_button();

            GUILayout.BeginVertical();

            GUILayout.BeginHorizontal();
            bool pitchIsActive = cruiseController.Active && cruiseController.vertical_control;
            // Ignore the result from GUILayout.Toggle; these buttons don't do anything yet.
            GUILayout.Toggle(pitchIsActive, "PITCH", GUIStyles.toggleButtonStyle);
            GUILayout.Label("ALTITUDE", GUILayout.Width(200));
            int oldAltitude = Mathf.RoundToInt(cruiseController.desired_altitude.Value);
            int newAltitude =
                Odospinner.Paint(oldAltitude, minValue: 0, maxValue: 70000, wrapAround: false);
            if (newAltitude != oldAltitude)
            {
                cruiseController.vertical_control = true;
                cruiseController.height_mode = CruiseController.HeightMode.Altitude;
                cruiseController.desired_altitude.Value = newAltitude;
            }
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Toggle(latNavController.Active, "ROLL", GUIStyles.toggleButtonStyle);
            GUILayout.Label("TRACK", GUILayout.Width(200));
            int oldHeading = Mathf.RoundToInt(latNavController.desired_course.Value);
            int newHeading =
                Odospinner.Paint(oldHeading, minValue: 0, maxValue: 359, wrapAround: true);
            if (newHeading != oldHeading)
            {
                latNavController.current_mode = LateralNavigationController.CruiseMode.CourseHold;
                latNavController.desired_course.Value = newHeading;
            }
            GUILayout.EndHorizontal();

            // cruiseController.Active = _altitudeAndHeadingEnabled;

            GUILayout.EndVertical();

            GUI.DragWindow();
        }

        protected override void OnActivate() { }
        protected override void OnDeactivate() { }

        public override void ApplyControl(FlightCtrlState cntrl) { }
    }
}
