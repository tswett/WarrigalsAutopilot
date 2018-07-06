using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace AtmosphereAutopilot
{
    public sealed class LateralNavigationController : AutopilotModule
    {
        public CruiseMode current_mode = CruiseMode.LevelFlight;
        /// <summary>axis to rotate around in level flight mode</summary>
        public Vector3d circle_axis = Vector3d.zero;

        [VesselSerializable("desired_course_field")]
        public DelayedFieldFloat desired_course = new DelayedFieldFloat(90.0f, "G4");

        [VesselSerializable("desired_latitude_field")]
        public DelayedFieldFloat desired_latitude = new DelayedFieldFloat(-0.0486178f, "#0.0000");  // latitude of KSC runway, west end (default position for launched vessels)

        [VesselSerializable("desired_longitude_field")]
        public DelayedFieldFloat desired_longitude = new DelayedFieldFloat(-74.72444f, "#0.0000");  // longitude of KSC runway, west end (default position for launched vessels)

        public Waypoint current_waypt = new Waypoint();

        public double dist_to_dest = 0.0;

        FlightModel imodel;

        internal LateralNavigationController(Vessel v)
            : base(v, 1056280730, "Lateral navigation controller")
        { }

        public override void InitializeDependencies(Dictionary<Type, AutopilotModule> modules)
        {
            imodel = modules[typeof(FlightModel)] as FlightModel;
        }

        protected override void OnActivate()
        {
            // let's set new circle axis
            if (vessel.srfSpeed > 5.0)
                circle_axis = Vector3d.Cross(vessel.srf_velocity, vessel.GetWorldPos3D() - vessel.mainBody.position).normalized;
            else
                circle_axis = Vector3d.Cross(vessel.ReferenceTransform.up, vessel.GetWorldPos3D() - vessel.mainBody.position).normalized;
        }

        protected override void OnDeactivate()
        {
        }

        internal bool LevelFlightMode
        {
            get { return current_mode == CruiseMode.LevelFlight; }
            set
            {
                if (value)
                {
                    if (current_mode != CruiseMode.LevelFlight)
                    {
                        // let's set new circle axis
                        circle_axis = Vector3d.Cross(vessel.srf_velocity, vessel.GetWorldPos3D() - vessel.mainBody.position).normalized;
                    }
                    current_mode = CruiseMode.LevelFlight;
                }
            }
        }

        internal bool CourseHoldMode
        {
            get { return current_mode == CruiseMode.CourseHold; }
            set
            {
                if (value)
                {
                    if (Math.Abs(vessel.latitude) > 80.0)
                        return;
                    if (current_mode != CruiseMode.CourseHold)
                    {
                        // TODO
                    }
                    current_mode = CruiseMode.CourseHold;
                }
            }
        }

        internal bool waypoint_entered = false;
        internal bool WaypointMode
        {
            get { return current_mode == CruiseMode.Waypoint; }
            set
            {
                if (value)
                {
                    if ((current_mode != CruiseMode.Waypoint) && !waypoint_entered)
                    {
                        if (this.Active)
                        {
                            circle_axis =
                                Vector3d.Cross(vessel.srf_velocity, vessel.GetWorldPos3D() - vessel.mainBody.position).normalized;
                            start_picking_waypoint();
                        }
                        else
                            MessageManager.post_quick_message("Can't pick waypoint when the Cruise Flight controller is disabled");
                    }
                    current_mode = CruiseMode.Waypoint;
                }
            }
        }

        internal bool picking_waypoint = false;
        internal void start_picking_waypoint()
        {
            MapView.EnterMapView();
            MessageManager.post_quick_message("Pick waypoint");
            picking_waypoint = true;
        }

        // TODO This method has way too many parameters.
        public void ApplyControl(
            FlightCtrlState cntrl, ref Vector3d desired_velocity, Vector3d planet2vesNorm)
        {
            switch (current_mode)
            {
                default:
                case CruiseMode.LevelFlight:
                    // simply select velocity from axis
                    desired_velocity = Vector3d.Cross(planet2vesNorm, circle_axis);
                    handle_wide_turn(ref desired_velocity, planet2vesNorm);
                    break;

                case CruiseMode.CourseHold:
                    if (Math.Abs(vessel.latitude) > 80.0)
                    {
                        // we're too close to poles, let's switch to level flight
                        LevelFlightMode = true;
                        goto case CruiseMode.LevelFlight;
                    }
                    // get direction vector form course
                    Vector3d north = vessel.mainBody.RotationAxis;
                    Vector3d north_projected = Vector3.ProjectOnPlane(north, planet2vesNorm);
                    QuaternionD rotation = QuaternionD.AngleAxis(desired_course, planet2vesNorm);
                    desired_velocity = rotation * north_projected;
                    handle_wide_turn(ref desired_velocity, planet2vesNorm);
                    break;

                case CruiseMode.Waypoint:
                    if (!waypoint_entered)
                    {
                        // goto simple level flight
                        goto case CruiseMode.LevelFlight;
                    }
                    else
                    {
                        // set new axis
                        Vector3d world_target_pos = vessel.mainBody.GetWorldSurfacePosition(desired_latitude, desired_longitude, vessel.altitude);
                        dist_to_dest = Vector3d.Distance(world_target_pos, vessel.ReferenceTransform.position);
                        if (dist_to_dest > 10000.0)
                        {
                            double radius = vessel.mainBody.Radius;
                            dist_to_dest = Math.Acos(1 - (dist_to_dest * dist_to_dest) / (2 * radius * radius)) * radius;
                        }
                        if (dist_to_dest < 200.0)
                        {
                            // we're too close to target, let's switch to level flight
                            LevelFlightMode = true;
                            picking_waypoint = false;
                            MessageManager.post_quick_message("Waypoint reached");
                            goto case CruiseMode.LevelFlight;
                        }
                        // set new axis according to waypoint
                        circle_axis = Vector3d.Cross(world_target_pos - vessel.mainBody.position, vessel.GetWorldPos3D() - vessel.mainBody.position).normalized;
                        goto case CruiseMode.LevelFlight;
                    }
            }
        }

        public void handle_wide_turn(ref Vector3d desired_velocity, Vector3d planet2vesNorm)
        {
            Vector3d hor_vel = imodel.surface_v - Vector3d.Project(imodel.surface_v, planet2vesNorm);
            if (Vector3d.Dot(hor_vel.normalized, desired_velocity.normalized) < Math.Cos(0.5))
            {
                // we're turning for more than 45 degrees, let's force the turn to be horizontal
                Vector3d right_turn = Vector3d.Cross(planet2vesNorm, imodel.surface_v);
                double sign = Math.Sign(Vector3d.Dot(right_turn, desired_velocity));
                if (sign == 0.0)
                    sign = 1.0;
                desired_velocity = right_turn.normalized * sign * Math.Tan(0.5) + hor_vel.normalized;
            }
        }

        bool need_to_show_course = false;
        float course_change_counter = 0.0f;

        [AutoGuiAttr("hotkey_course_speed", true, "G4")]
        [GlobalSerializable("hotkey_course_sens")]
        public static float hotkey_course_sens = 60.0f;

        public override void OnUpdate()
        {
            if (picking_waypoint)
            {
                OnUpdatePickingWaypoint();
                return;
            }

            // input shenanigans
            if (CruiseController.use_keys && !FlightDriver.Pause
                && InputLockManager.IsUnlocked(ControlTypes.YAW))
            {
                // Yaw (Course)
                bool yaw_key_pressed = false;
                float yaw_change_sign = 0.0f;
                if (GameSettings.YAW_RIGHT.GetKey() && !GameSettings.MODIFIER_KEY.GetKey())
                {
                    yaw_key_pressed = true;
                    yaw_change_sign = 1.0f;
                }
                else if (GameSettings.YAW_LEFT.GetKey() && !GameSettings.MODIFIER_KEY.GetKey())
                {
                    yaw_key_pressed = true;
                    yaw_change_sign = -1.0f;
                }

                if (yaw_key_pressed)
                {
                    float setpoint = desired_course;
                    float new_setpoint = setpoint + yaw_change_sign * hotkey_course_sens * Time.deltaTime;
                    if (new_setpoint > 360.0f)
                        new_setpoint -= 360.0f;
                    if (new_setpoint < 0.0f)
                        new_setpoint = 360.0f + new_setpoint;
                    desired_course.Value = new_setpoint;
                    need_to_show_course = true;
                    course_change_counter = 0.0f;
                }

                if (need_to_show_course)
                    course_change_counter += Time.deltaTime;
                if (course_change_counter > 1.0f)
                {
                    course_change_counter = 0;
                    need_to_show_course = false;
                }
            }
            else
            {
                need_to_show_course = false;
                course_change_counter = 0;
            }
        }

        void OnUpdatePickingWaypoint()
        {
            if (!HighLogic.LoadedSceneIsFlight || !MapView.MapIsEnabled)
            {
                // we left map without picking
                MessageManager.post_quick_message("Cancelled");
                picking_waypoint = false;
                AtmosphereAutopilot.Instance.mainMenuGUIUpdate();
                return;
            }
            // Thanks MechJeb!
            if (Input.GetMouseButtonDown(0) && !window.Contains(Input.mousePosition))
            {
                Ray mouseRay = PlanetariumCamera.Camera.ScreenPointToRay(Input.mousePosition);
                mouseRay.origin = ScaledSpace.ScaledToLocalSpace(mouseRay.origin);
                Vector3d relOrigin = mouseRay.origin - vessel.mainBody.position;
                Vector3d relSurfacePosition;
                double curRadius = vessel.mainBody.pqsController.radiusMax;
                if (PQS.LineSphereIntersection(relOrigin, mouseRay.direction, curRadius, out relSurfacePosition))
                {
                    Vector3d surfacePoint = vessel.mainBody.position + relSurfacePosition;
                    current_waypt.longitude = vessel.mainBody.GetLongitude(surfacePoint);
                    current_waypt.latitude = vessel.mainBody.GetLatitude(surfacePoint);
                    picking_waypoint = false;
                    waypoint_entered = true;

                    desired_latitude.Value = (float)current_waypt.latitude;
                    desired_longitude.Value = (float)current_waypt.longitude;

                    dist_to_dest = Vector3d.Distance(surfacePoint, vessel.ReferenceTransform.position);
                    AtmosphereAutopilot.Instance.mainMenuGUIUpdate();
                    MessageManager.post_quick_message("Picked");
                }
                else
                {
                    MessageManager.post_quick_message("Missed");
                }
            }
        }

        protected override void OnGUICustomAlways()
        {
            if (need_to_show_course)
            {
                Rect rect = new Rect(Screen.width / 2.0f - 80.0f, 140.0f, 160.0f, 20.0f);
                string str = "course = " + desired_course.Value.ToString("G4");
                GUI.Label(rect, str, GUIStyles.hoverLabel);
            }

            desired_course.OnUpdate();
            desired_latitude.OnUpdate();
            desired_longitude.OnUpdate();
        }

        public enum CruiseMode
        {
            LevelFlight,
            CourseHold,
            Waypoint
        }
    }
}
