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

        public void ApplyControl(
            FlightCtrlState cntrl, ref Vector3d desired_velocity, Vector3d planet2vesNorm,
            float desired_course, float desired_latitude, float desired_longitude,
            ref double dist_to_dest, bool waypoint_entered, ref bool picking_waypoint)
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

        public enum CruiseMode
        {
            LevelFlight,
            CourseHold,
            Waypoint
        }
    }
}
