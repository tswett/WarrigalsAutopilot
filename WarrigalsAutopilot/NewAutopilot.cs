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

        //FlightModel flightModel;
        //DirectorController directorController;

        public override void InitializeDependencies(Dictionary<Type, AutopilotModule> modules)
        {
            //flightModel = modules[typeof(FlightModel)] as FlightModel;
            //directorController = modules[typeof(DirectorController)] as DirectorController;
        }

        protected override void _drawGUI(int id)
        {
            close_button();

            GUILayout.Label("Placeholder for new WAP GUI");

            GUI.DragWindow();
        }

        protected override void OnActivate() { }
        protected override void OnDeactivate() { }

        public override void ApplyControl(FlightCtrlState cntrl) { }
    }
}
