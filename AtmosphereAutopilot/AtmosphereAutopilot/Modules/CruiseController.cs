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
using UnityEngine;
using static AtmosphereAutopilot.LateralNavigationController;

namespace AtmosphereAutopilot
{
    public struct Waypoint
    {
        public Waypoint(double longt, double lat)
        {
            longitude = longt;
            latitude = lat;
        }
        public double longitude;
        public double latitude;
    }

    /// <summary>
    /// Manages cruise flight modes, like heading and altitude holds
    /// </summary>
    public sealed class CruiseController : StateController
    {
        internal CruiseController(Vessel v)
            : base(v, "Cruise Flight controller", 88437226)
        { }

        FlightModel imodel;
        DirectorController dir_c;
        ProgradeThrustController thrust_c;
        LateralNavigationController latNavController;

        public override void InitializeDependencies(Dictionary<Type, AutopilotModule> modules)
        {
            imodel = modules[typeof(FlightModel)] as FlightModel;
            dir_c = modules[typeof(DirectorController)] as DirectorController;
            thrust_c = modules[typeof(ProgradeThrustController)] as ProgradeThrustController;
            latNavController = modules[typeof(LateralNavigationController)] as LateralNavigationController;
        }

        protected override void OnActivate()
        {
            dir_c.Activate();
            thrust_c.Activate();
            imodel.Activate();
            latNavController.Activate();
            MessageManager.post_status_message("Cruise Flight enabled");
        }

        protected override void OnDeactivate()
        {
            dir_c.Deactivate();
            thrust_c.Deactivate();
            imodel.Deactivate();
            latNavController.Deactivate();
            MessageManager.post_status_message("Cruise Flight disabled");
        }

        Vector3d desired_velocity = Vector3d.zero;
        Vector3d planet2ves = Vector3d.zero;
        Vector3d planet2vesNorm = Vector3d.zero;
        Vector3d desired_vert_acc = Vector3d.zero;

        // centrifugal acceleration to stay on desired altitude
        Vector3d level_acc = Vector3d.zero;

        public override void ApplyControl(FlightCtrlState cntrl)
        {
            if (vessel.LandedOrSplashed)
                return;

            if (thrust_c.spd_control_enabled)
                thrust_c.ApplyControl(cntrl, thrust_c.setpoint.mps());

            desired_velocity = Vector3d.zero;
            planet2ves = vessel.ReferenceTransform.position - vessel.mainBody.position;
            planet2vesNorm = planet2ves.normalized;
            desired_vert_acc = Vector3d.zero;

            // centrifugal acceleration to stay on desired altitude
            level_acc = -planet2vesNorm * (imodel.surface_v - Vector3d.Project(imodel.surface_v, planet2vesNorm)).sqrMagnitude / planet2ves.magnitude;

            latNavController.ApplyControl(cntrl, ref desired_velocity, planet2vesNorm);

            if (vertical_control)
            {
                if (height_mode == HeightMode.Altitude)
                    desired_velocity = account_for_height(desired_velocity);
                else
                    desired_velocity = account_for_vertical_vel(desired_velocity);
            }

            if (use_keys)
            {
                ControlUtils.neutralize_user_input(cntrl, PITCH);
                ControlUtils.neutralize_user_input(cntrl, YAW);
            }

            double old_str = dir_c.strength;
            dir_c.strength *= strength_mult;
            dir_c.ApplyControl(cntrl, desired_velocity, level_acc + desired_vert_acc);
            dir_c.strength = old_str;
        }

        public CruiseMode current_mode
        {
            get => latNavController.current_mode;
            set => latNavController.current_mode = value;
        }

        public enum HeightMode
        {
            Altitude,
            VerticalSpeed
        }

        public HeightMode height_mode = HeightMode.Altitude;

        [AutoGuiAttr("Director controller GUI", true)]
        public bool DircGUI { get { return dir_c.IsShown(); } set { if (value) dir_c.ShowGUI(); else dir_c.UnShowGUI(); } }

        [AutoGuiAttr("Thrust controller GUI", true)]
        public bool PTCGUI { get { return thrust_c.IsShown(); } set { if (value) thrust_c.ShowGUI(); else thrust_c.UnShowGUI(); } }

        [VesselSerializable("vertical_control")]
        public bool vertical_control = false;

        [VesselSerializable("desired_altitude_field")]
        public DelayedFieldFloat desired_altitude = new DelayedFieldFloat(1000.0f, "G5");

        [VesselSerializable("desired_vertspeed_field")]
        public DelayedFieldFloat desired_vertspeed = new DelayedFieldFloat(0.0f, "G4");

        [GlobalSerializable("preudo_flc")]
        [VesselSerializable("preudo_flc")]
        [AutoGuiAttr("pseudo-FLC", true)]
        public bool pseudo_flc = true;

        [VesselSerializable("flc_margin")]
        [AutoGuiAttr("flc_margin", true, "G4")]
        public double flc_margin = 20.0;

        [VesselSerializable("strength_mult")]
        [AutoGuiAttr("strength_mult", true, "G5")]
        public double strength_mult = 0.75;

        [VesselSerializable("height_relax_time")]
        [AutoGuiAttr("height_relax_time", true, "G5")]
        public double height_relax_time = 6.0;

        [VesselSerializable("height_relax_Kp")]
        [AutoGuiAttr("height_relax_Kp", true, "G5")]
        public double height_relax_Kp = 0.3;

        [VesselSerializable("max_climb_angle")]
        [AutoGuiAttr("max_climb_angle", true, "G5")]
        public double max_climb_angle = 30.0;

        double filtered_drag = 0.0;

        Vector3d account_for_vertical_vel(Vector3d desired_direction)
        {
            Vector3d res = desired_direction.normalized * vessel.horizontalSrfSpeed + planet2vesNorm * desired_vertspeed;
            return res.normalized;
        }

        Vector3d account_for_height(Vector3d desired_direction)
        {
            double cur_alt = vessel.altitude;
            double height_error = desired_altitude - cur_alt;
            double acc = Vector3.Dot(imodel.gravity_acc + imodel.noninert_acc, -planet2vesNorm);    // free-fall vertical acceleration
            double height_relax_frame = 0.5 * acc * height_relax_time * height_relax_time;

            double relax_transition_k = 0.0;
            double des_vert_speed = 0.0;
            double relax_vert_speed = 0.0;
            Vector3d res = Vector3d.zero;

            Vector3d proportional_acc = Vector3d.zero;
            double cur_vert_speed = Vector3d.Dot(imodel.surface_v, planet2vesNorm);
            if (Math.Abs(height_error) < height_relax_frame)
            {
                relax_transition_k = Common.Clamp(2.0 * (height_relax_frame - Math.Abs(height_error)), 0.0, 1.0);
                // we're in relaxation frame
                relax_vert_speed = height_relax_Kp * height_error;
                // exponential descent
                if (cur_vert_speed * height_error > 0.0)
                    proportional_acc = -planet2vesNorm * height_relax_Kp * cur_vert_speed;
            }

            // let's assume parabolic ascent\descend
            Vector3d parabolic_acc = Vector3d.zero;
            if (height_error >= 0.0)
            {
                des_vert_speed = Math.Sqrt(acc * height_error);
                if (cur_vert_speed > 0.0)
                    parabolic_acc = -planet2vesNorm * 0.5 * cur_vert_speed * cur_vert_speed / height_error;
            }
            else
            {
                double vert_acc_descent = 2.0 * Math.Min(-5.0, acc - dir_c.strength * strength_mult * dir_c.max_lift_acc * 0.5);
                des_vert_speed = -Math.Sqrt(vert_acc_descent * height_error);
                if (cur_vert_speed < 0.0)
                    parabolic_acc = -planet2vesNorm * 0.5 * cur_vert_speed * cur_vert_speed / height_error;
            }

            // speed control portion for ascend
            double effective_max_climb_angle = max_climb_angle;
            if (thrust_c.spd_control_enabled && (height_error >= 0.0))
            {
                if (pseudo_flc)
                {
                    filtered_drag = Common.simple_filter(thrust_c.drag_estimate, filtered_drag, 5.0);
                    if (thrust_c.estimated_max_thrust > filtered_drag)
                    {
                        double sin = Vector3d.Dot(-planet2vesNorm, imodel.gravity_acc + imodel.noninert_acc) / (thrust_c.estimated_max_thrust - filtered_drag);
                        if (sin < 0.0 || sin >= 1.0)
                            effective_max_climb_angle = Math.Min(Math.Asin(sin), max_climb_angle);
                    }
                    else
                        effective_max_climb_angle = 1.0;

                    double spd_diff = (imodel.surface_v_magnitude - thrust_c.setpoint.mps());
                    if (spd_diff < -flc_margin)
                        effective_max_climb_angle *= 0.0;
                    else if (spd_diff < 0.0)
                        effective_max_climb_angle *= (spd_diff + flc_margin) / flc_margin;
                }
                else
                    effective_max_climb_angle *= Math.Max(0.0, Math.Min(1.0, vessel.srfSpeed / thrust_c.setpoint.mps()));
            }

            double max_vert_speed = vessel.horizontalSrfSpeed * Math.Tan(effective_max_climb_angle * dgr2rad);
            bool apply_acc = Math.Abs(des_vert_speed) < max_vert_speed;
            des_vert_speed = Common.Clamp(des_vert_speed, max_vert_speed);
            res = desired_direction.normalized * vessel.horizontalSrfSpeed + planet2vesNorm * Common.lerp(des_vert_speed, relax_vert_speed, relax_transition_k);
            if (apply_acc)
                desired_vert_acc = parabolic_acc * (1.0 - relax_transition_k) + proportional_acc * relax_transition_k;
            return res.normalized;
        }

        internal bool LevelFlightMode
        {
            get => latNavController.LevelFlightMode;
            set => latNavController.LevelFlightMode = value;
        }

        bool CourseHoldMode
        {
            get => latNavController.CourseHoldMode;
            set => latNavController.CourseHoldMode = value;
        }

        internal bool WaypointMode
        {
            get => latNavController.WaypointMode;
            set => latNavController.WaypointMode = value;
        }

        bool AltitudeMode
        {
            get { return height_mode == HeightMode.Altitude; }
            set
            {
                if (value)
                    height_mode = HeightMode.Altitude;
                else
                    height_mode = HeightMode.VerticalSpeed;
            }
        }

        bool VerticalSpeedMode
        {
            get { return height_mode == HeightMode.VerticalSpeed; }
            set
            {
                if (!value)
                    height_mode = HeightMode.Altitude;
                else
                    height_mode = HeightMode.VerticalSpeed;
            }
        }

        static bool advanced_options = false;

        protected override void _drawGUI(int id)
        {
            close_button();
            GUILayout.BeginVertical();

            // cruise flight control modes

            LevelFlightMode = GUILayout.Toggle(LevelFlightMode, "Level", GUIStyles.toggleButtonStyle);

            GUILayout.Space(5.0f);

            CourseHoldMode = GUILayout.Toggle(CourseHoldMode, "Track",    GUIStyles.toggleButtonStyle);
            GUILayout.BeginHorizontal();
            GUILayout.Label("desired course", GUIStyles.labelStyleLeft);
            latNavController.desired_course.DisplayLayout(GUIStyles.textBoxStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(5.0f);

            string waypoint_btn_str;
            if (WaypointMode)
                waypoint_btn_str = "WPT " + (latNavController.dist_to_dest / 1000.0).ToString("#0.0") + " km";
            else
                waypoint_btn_str = "Waypoint";
            WaypointMode = GUILayout.Toggle(WaypointMode, waypoint_btn_str,
                GUIStyles.toggleButtonStyle);
            GUILayout.BeginHorizontal();
            latNavController.desired_latitude.DisplayLayout(GUIStyles.textBoxStyle, GUILayout.Width(60.0f));
            latNavController.desired_longitude.DisplayLayout(GUIStyles.textBoxStyle, GUILayout.Width(60.0f));
            if (GUILayout.Button("Pick", GUIStyles.toggleButtonStyle) && !latNavController.picking_waypoint)
            {
                if (this.Active)
                    latNavController.start_picking_waypoint();
                else
                    MessageManager.post_quick_message("Can't pick waypoint when the Cruise Flight controller is disabled");
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10.0f);

            // speed

            thrust_c.SpeedCtrlGUIBlock();

            GUILayout.Space(10.0f);

            // vertical motion

            vertical_control = GUILayout.Toggle(vertical_control, "Vertical motion", GUIStyles.toggleButtonStyle);
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            AltitudeMode = GUILayout.Toggle(AltitudeMode, "Altitude", GUIStyles.toggleButtonStyle);     // GUILayout.Width(90.0f)
            desired_altitude.DisplayLayout(GUIStyles.textBoxStyle);                                     // GUILayout.Width(90.0f)
            GUILayout.EndVertical();
            GUILayout.BeginVertical();
            VerticalSpeedMode = GUILayout.Toggle(VerticalSpeedMode, "Vertical speed", GUIStyles.toggleButtonStyle);
            desired_vertspeed.DisplayLayout(GUIStyles.textBoxStyle);
            GUILayout.EndVertical();
            GUILayout.EndHorizontal();

            GUILayout.Space(10.0f);

            // status

            //GUILayout.Label("Status", GUIStyles.labelStyleCenter);
            //GUILayout.BeginHorizontal();
            //GUILayout.BeginVertical();
            //GUILayout.Label("Latitude", GUIStyles.labelStyleCenter);
            //GUILayout.Label(vessel.latitude.ToString("G6"), GUIStyles.labelStyleCenter);
            //GUILayout.EndVertical();
            //GUILayout.BeginVertical();
            //GUILayout.Label("Longitude", GUIStyles.labelStyleCenter);
            //GUILayout.Label(vessel.longitude.ToString("G7"), GUIStyles.labelStyleCenter);
            //GUILayout.EndVertical();
            //GUILayout.BeginVertical();
            //if (WaypointMode)
            //{
            //    GUILayout.Label("Dist (km)", GUIStyles.labelStyleCenter);
            //    GUILayout.Label((dist_to_dest / 1000.0).ToString("#0.0"), GUIStyles.labelStyleCenter);
            //}
            //else
            //{
            //    GUILayout.Label("Alt (m)", GUIStyles.labelStyleCenter);
            //    GUILayout.Label(vessel.altitude.ToString("G5") + " m", GUIStyles.labelStyleCenter);
            //}
            //GUILayout.EndVertical();
            //GUILayout.EndHorizontal();

            //GUILayout.Space(10.0f);

            // advanced options

            bool adv_o = advanced_options;
            advanced_options = GUILayout.Toggle(advanced_options, "Advanced options", GUIStyles.toggleButtonStyle);
            if (advanced_options)
            {
                GUILayout.Space(5.0f);
                AutoGUI.AutoDrawObject(this);
            }
            else if (adv_o)
                window.height = 100.0f;

            GUILayout.EndVertical();
            GUI.DragWindow();
        }

        public override void OnUpdate()
        {
            if (Input.GetKeyDown(switch_key_mode))
            {
                use_keys = !use_keys;
                MessageManager.post_status_message(use_keys ? "CF key input mode enabled" : "CF key input mode disabled");
            }

            if (Input.GetKeyDown(vertical_control_key))
            {
                vertical_control = !vertical_control;
                MessageManager.post_status_message(use_keys ? "Vertical motion control enabled" : "Vertical motion control disabled");
            }

            if (Input.GetKeyDown(toggle_vertical_setpoint_type_key))
            {
                AltitudeMode = !AltitudeMode;
                MessageManager.post_status_message(AltitudeMode ? "Altitude control" : "Vertical speed control");
            }

            // input shenanigans
            if (use_keys && !FlightDriver.Pause && InputLockManager.IsUnlocked(ControlTypes.PITCH))
            {
                bool pitch_key_pressed = false;
                float pitch_change_sign = 0.0f;
                // Pitch
                if (GameSettings.PITCH_UP.GetKey() && !GameSettings.MODIFIER_KEY.GetKey())
                {
                    pitch_change_sign = 1.0f;
                    pitch_key_pressed = true;
                }
                else if (GameSettings.PITCH_DOWN.GetKey() && !GameSettings.MODIFIER_KEY.GetKey())
                {
                    pitch_change_sign = -1.0f;
                    pitch_key_pressed = true;
                }

                if (pitch_key_pressed)
                {
                    if (height_mode == HeightMode.Altitude)
                    {
                        float setpoint = desired_altitude;
                        float new_setpoint = setpoint + pitch_change_sign * hotkey_altitude_sens * Time.deltaTime * setpoint;
                        desired_altitude.Value = new_setpoint;
                    }
                    else
                    {
                        float setpoint = desired_vertspeed;
                        float magnetic_mult = Mathf.Abs(desired_vertspeed) < 10.0f ? 0.3f : 1.0f;
                        float new_setpoint = setpoint + pitch_change_sign * hotkey_vertspeed_sens * Time.deltaTime * magnetic_mult;
                        desired_vertspeed.Value = new_setpoint;
                    }
                    need_to_show_altitude = true;
                    altitude_change_counter = 0.0f;
                    AtmosphereAutopilot.Instance.mainMenuGUIUpdate();
                }

                if (need_to_show_altitude)
                {
                    altitude_change_counter += Time.deltaTime;
                    if (height_mode == HeightMode.VerticalSpeed && altitude_change_counter > 0.2f)
                        if (Mathf.Abs(desired_vertspeed) < hotkey_vertspeed_snap)
                            desired_vertspeed.Value = 0.0f;
                }
                if (altitude_change_counter > 1.0f)
                {
                    altitude_change_counter = 0;
                    need_to_show_altitude = false;
                }
            }
            else
            {
                need_to_show_altitude = false;
                altitude_change_counter = 0;
            }
        }

        [GlobalSerializable("use_keys")]
        [AutoGuiAttr("use keys", true)]
        public static bool use_keys = true;

        [GlobalSerializable("switch_key_mode")]
        [AutoHotkeyAttr("CF keys input mode")]
        static KeyCode switch_key_mode = KeyCode.RightAlt;

        [GlobalSerializable("vertical_control_key")]
        [AutoHotkeyAttr("CF vertical control")]
        static KeyCode vertical_control_key = KeyCode.None;

        [GlobalSerializable("toggle_vertical_setpoint_type_key")]
        [AutoHotkeyAttr("CF altitude\\vertical speed")]
        static KeyCode toggle_vertical_setpoint_type_key = KeyCode.None;

        bool need_to_show_altitude = false;
        float altitude_change_counter = 0.0f;

        [AutoGuiAttr("hotkey_altitude_speed", true, "G4")]
        [GlobalSerializable("hotkey_altitude_sens")]
        public static float hotkey_altitude_sens = 0.8f;

        [AutoGuiAttr("hotkey_vertspeed_speed", true, "G4")]
        [GlobalSerializable("hotkey_vertspeed_sens")]
        public static float hotkey_vertspeed_sens = 30.0f;

        [AutoGuiAttr("hotkey_vertspeed_snap", true, "G4")]
        [GlobalSerializable("hotkey_vertspeed_snap")]
        public static float hotkey_vertspeed_snap = 0.5f;


        protected override void OnGUICustomAlways()
        {
            if (need_to_show_altitude)
            {
                Rect rect = new Rect(Screen.width / 2.0f - 80.0f, 160.0f, 160.0f, 20.0f);
                string str = null;
                if (height_mode == HeightMode.Altitude)
                    str = "Altitude = " + desired_altitude.Value.ToString("G5");
                else
                    str = "Vert speed = " + desired_vertspeed.Value.ToString("G4");
                GUI.Label(rect, str, GUIStyles.hoverLabel);
            }

            desired_altitude.OnUpdate();
            desired_vertspeed.OnUpdate();
        }
    }
}
