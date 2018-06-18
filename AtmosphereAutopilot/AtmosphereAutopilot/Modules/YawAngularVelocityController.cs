/*
Atmosphere Autopilot, plugin for Kerbal Space Program.
Copyright (C) 2015-2016, Baranin Alexander aka Boris-Barboris.
 
Atmosphere Autopilot is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
Atmosphere Autopilot is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with Atmosphere Autopilot.  If not, see <http://www.gnu.org/licenses/>. 
*/

using System;
using System.Collections.Generic;

namespace AtmosphereAutopilot
{
    public sealed class YawAngularVelocityController : PitchYawAngularVelocityController
    {
        internal YawAngularVelocityController(Vessel vessel)
            : base(vessel, "Yaw ang vel controller", 1234446, YAW)
        { }

        public override void InitializeDependencies(Dictionary<Type, AutopilotModule> modules)
        {
            base.InitializeDependencies(modules);
            this.acc_controller = modules[typeof(YawAngularAccController)] as YawAngularAccController;
            this.lin_model = imodel.yaw_rot_model_gen;
        }
    }
}
