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
using System.Linq;
using System.Text;

namespace AtmosphereAutopilot
{

    /// <summary>
    /// Controls angular velocity
    /// </summary>
    public abstract class AngularVelAdaptiveController : AutopilotModule
    {
        protected int axis;

        protected FlightModel imodel;
        protected AngularAccAdaptiveController acc_controller;
        public bool user_controlled = false;

        /// <summary>
        /// Create controller instance
        /// </summary>
        /// <param name="vessel">Vessel to control</param>
        /// <param name="module_name">Name of controller</param>
        /// <param name="wnd_id">unique for types window id</param>
        /// <param name="axis">Pitch = 0, roll = 1, yaw = 2</param>
        /// <param name="model">Flight model instance for adaptive control</param>
        protected AngularVelAdaptiveController(Vessel vessel, string module_name,
            int wnd_id, int axis)
            : base(vessel, wnd_id, module_name)
        {
            this.axis = axis;
            ApplyTrim = false;
        }

        public override void InitializeDependencies(Dictionary<Type, AutopilotModule> modules)
        {
            this.imodel = modules[typeof(FlightModel)] as FlightModel;
        }

        protected override void OnActivate()
        {
            imodel.Activate();
            acc_controller.Activate();
        }

        protected override void OnDeactivate()
        {
            imodel.Deactivate();
            acc_controller.Deactivate();
        }

        double time_in_regime = 0.0;

        [AutoGuiAttr("angular vel", false, "G8")]
        protected float vel;

        [AutoGuiAttr("output acceleration", false, "G8")]
        protected float output_acc;

        //[AutoGuiAttr("Kp", true, "G8")]
        float Kp = 8.0f;

        [GlobalSerializable("user_input_deriv_clamp")]
        [AutoGuiAttr("Input deriv limit", true, "G6")]
        public float user_input_deriv_clamp = 5.0f;

        [GlobalSerializable("watch_precision_mode")]
        [AutoGuiAttr("watch precision mode", true)]
        public bool watch_precision_mode = true;

        [GlobalSerializable("precision_mode_factor")]
        //[AutoGuiAttr("precision_mode_factor", true)]
        public float precision_mode_factor = 0.33f;

        [AutoGuiAttr("prev_input", false, "G6")]
        protected float prev_input;

        /// <summary>
        /// Main control function
        /// </summary>
        /// <param name="cntrl">Control state to change</param>
        public float ApplyControl(FlightCtrlState cntrl, float target_value, float target_acc = 0.0f)
        {
            vel = imodel.AngularVel(axis);                  // get angular velocity

            float user_input = ControlUtils.get_neutralized_user_input(cntrl, axis);

            if (user_input != 0.0f || user_controlled)
            {
                // user is interfering with control
                float clamp = (watch_precision_mode && FlightInputHandler.fetch.precisionMode) ?
                    precision_mode_factor * user_input_deriv_clamp * TimeWarp.fixedDeltaTime :
                    user_input_deriv_clamp * TimeWarp.fixedDeltaTime;
                if (watch_precision_mode && FlightInputHandler.fetch.precisionMode)
                    user_input *= precision_mode_factor;
                float delta_input = Common.Clampf(user_input - prev_input, clamp);
                user_input = prev_input + delta_input;
                prev_input = user_input;
                desired_v = user_input * max_v_construction;
                user_controlled = true;
            }
            else
            {
                // control from above
                desired_v = Common.Clampf(target_value, max_v_construction);
            }

            desired_v = process_desired_v(desired_v, user_controlled);      // moderation stage

            output_acc = get_desired_acc(desired_v) + target_acc;           // produce output acceleration

            // check if we're stable on given input value
            if (ApplyTrim && vessel == AtmosphereAutopilot.Instance.ActiveVessel)
            {
                if (Math.Abs(vel) < 0.002f)
                {
                    time_in_regime += TimeWarp.fixedDeltaTime;
                }
                else
                {
                    time_in_regime = 0.0;
                }

                if (time_in_regime >= 5.0)
                    ControlUtils.set_trim(axis, imodel.ControlSurfPosHistory(axis).Average());
            }

            acc_controller.ApplyControl(cntrl, output_acc);

            return output_acc;
        }

        [VesselSerializable("max_v_construction")]
        [AutoGuiAttr("Max v construction", true, "G6")]
        public float max_v_construction = 0.7f;

        protected virtual float process_desired_v(float des_v, bool user_input) { return des_v; }

        protected virtual float get_desired_acc(float des_v) { return Kp * (desired_v - vel); }

        [AutoGuiAttr("DEBUG desired_v", false, "G6")]
        protected float desired_v;

        [GlobalSerializable("ApplyTrim")]
        [AutoGuiAttr("ApplyTrim", true)]
        public bool ApplyTrim { get; set; }
    }
}
