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

namespace AtmosphereAutopilot
{
    public sealed class RollAngularVelocityController : AngularVelAdaptiveController
    {
        internal RollAngularVelocityController(Vessel vessel)
            : base(vessel, "Roll ang vel controller", 1234445, ROLL)
        {
            max_v_construction = 3.0f;
        }

        public override void InitializeDependencies(Dictionary<Type, AutopilotModule> modules)
        {
            base.InitializeDependencies(modules);
            this.acc_controller = modules[typeof(RollAngularAccController)] as RollAngularAccController;
        }

        /// <summary>
        /// "Equilibrium angular velocity on max/min input AoA flight regimes."
        /// </summary>
        [AutoGuiAttr("max_input_v", false, "G6")]
        public float max_input_v;

        /// <summary>
        /// "Equilibrium angular velocity on max/min input AoA flight regimes."
        /// </summary>
        [AutoGuiAttr("min_input_v", false, "G6")]
        public float min_input_v;

        /// <summary>
        /// "Used to filter out rapid changes or oscillations in flight model to provide more
        /// smooth boundary condition evolution."
        /// </summary>
        [AutoGuiAttr("moder_filter", true, "G6")]
        float moder_filter = 4.0f;

        Matrix state_mat = new Matrix(3, 1);
        Matrix input_mat = new Matrix(3, 1);

        /// <summary>
        /// True to automatically level the wings if the bank angle is less than a
        /// particular threshold.
        /// </summary>
        [VesselSerializable("wing_leveler")]
        [GlobalSerializable("wing_leveler")]
        [AutoGuiAttr("Wing leveler", true)]
        public bool wing_leveler = true;

        /// <summary>
        /// The bank angle below which the wings will automatically be leveled (if wing_leveler
        /// is true).
        /// </summary>
        [AutoGuiAttr("Snap angle", true, "G4")]
        public float leveler_snap_angle = 3.0f;

        [AutoGuiAttr("angle_btw_hor", false, "G5")]
        float angle_btw_hor;

        [AutoGuiAttr("angle_btw_hor_sin", false, "G5")]
        float angle_btw_hor_sin;

        float transit_max_v;

        [AutoGuiAttr("snapping_Kp", true, "G5")]
        public float snapping_Kp = 0.25f;

        public const float min_abs_angv = 0.05f;

        protected override float process_desired_v(float des_v, bool user_input)
        {
            float cur_aoa = imodel.AoA(YAW);

            // let's find maximum angular v on 0.0 AoA and 0.0 Yaw input from model
            if (Math.Abs(cur_aoa) < 0.3 && imodel.dyn_pressure > 100.0)
            {
                float new_max_input_v =
                    (float)((imodel.roll_rot_model_gen.C[0, 0] + imodel.roll_rot_model_gen.B[0, 0] +
                    imodel.roll_rot_model_gen.A[0, 1] + imodel.roll_rot_model_gen.A[0, 2]) /
                        -imodel.roll_rot_model_gen.A[0, 0]);
                float new_min_input_v =
                    (float)((imodel.roll_rot_model_gen.C[0, 0] - imodel.roll_rot_model_gen.B[0, 0] -
                    imodel.roll_rot_model_gen.A[0, 1] - imodel.roll_rot_model_gen.A[0, 2]) /
                        -imodel.roll_rot_model_gen.A[0, 0]);
                if (!float.IsInfinity(new_max_input_v) && !float.IsNaN(new_max_input_v) &&
                    !float.IsInfinity(new_min_input_v) && !float.IsNaN(new_min_input_v))
                {
                    // adequacy check
                    if (new_max_input_v < new_min_input_v || new_max_input_v < 0.0 || new_min_input_v > 0.0)
                    {
                        new_max_input_v = max_v_construction;
                        new_min_input_v = -max_v_construction;
                    }
                    new_max_input_v = Mathf.Max(min_abs_angv, new_max_input_v);
                    new_min_input_v = Mathf.Min(-min_abs_angv, new_min_input_v);
                    max_input_v = (float)Common.simple_filter(new_max_input_v, max_input_v, moder_filter);
                    min_input_v = (float)Common.simple_filter(new_min_input_v, min_input_v, moder_filter);
                }
                else
                {
                    max_input_v = max_v_construction;
                    min_input_v = -max_v_construction;
                }
            }
            else
            {
                max_input_v = max_v_construction;
                min_input_v = -max_v_construction;
            }

            // wing level snapping
            float snapping_vel = 0.0f;
            if (wing_leveler && user_input && des_v == 0.0f && kacc_quadr > 1e-6 && imodel.dyn_pressure > 0.0)
            {
                Vector3 planet2ves = (vessel.ReferenceTransform.position - vessel.mainBody.position).normalized;
                float zenith_angle = Vector3.Angle(planet2ves, vessel.ReferenceTransform.up);
                if (zenith_angle > 20.0f && zenith_angle < 160.0f && imodel.surface_v_magnitude > 10.0)
                {
                    Vector3 right_horizont_vector = Vector3.Cross(planet2ves, vessel.srf_velocity);
                    Vector3 right_vector = imodel.virtualRotation * Vector3.right;
                    Vector3 right_project = Vector3.ProjectOnPlane(right_vector, Vector3.Cross(vessel.srf_velocity, right_horizont_vector));
                    Vector3 roll_vector = Vector3.Cross(right_vector, right_project.normalized);
                    angle_btw_hor_sin = -Vector3.Dot(roll_vector, vessel.ReferenceTransform.up);
                    if (Math.Abs(angle_btw_hor_sin) <= Math.Sin(leveler_snap_angle * dgr2rad))
                    {
                        angle_btw_hor = Mathf.Asin(angle_btw_hor_sin);
                        float dt = TimeWarp.fixedDeltaTime;

                        // Non-overshooting velocity for leveler_snap_angle
                        float transit_max_angle = leveler_snap_angle * dgr2rad;
                        state_mat[0, 0] = 0.0;
                        state_mat[1, 0] = 1.0;
                        state_mat[2, 0] = 1.0;
                        input_mat[0, 0] = 1.0;
                        double acc = imodel.roll_rot_model_gen.eval_row(0, state_mat, input_mat);
                        float new_dyn_max_v =
                            (float)Math.Sqrt(transit_max_angle * acc);
                        if (!float.IsNaN(new_dyn_max_v))
                        {
                            new_dyn_max_v = Common.Clampf(new_dyn_max_v, max_v_construction);
                            transit_max_v = (float)Common.simple_filter(new_dyn_max_v, transit_max_v, moder_filter);
                            snapping_vel = snapping_Kp * angle_btw_hor / transit_max_angle * transit_max_v;
                            if (Math.Abs(snapping_vel) > Math.Abs(angle_btw_hor) / dt)
                                snapping_vel = angle_btw_hor / dt;
                        }
                    }
                }
            }

            // desired_v moderation section
            if (des_v >= 0.0f)
            {
                float normalized_des_v = user_input ? des_v / max_v_construction : des_v / Math.Min(max_input_v, max_v_construction);
                if (float.IsInfinity(normalized_des_v) || float.IsNaN(normalized_des_v))
                    normalized_des_v = 0.0f;
                normalized_des_v = Common.Clampf(normalized_des_v, 1.0f);
                float scaled_restrained_v = Math.Min(max_input_v, max_v_construction);
                des_v = normalized_des_v * scaled_restrained_v;
            }
            else
            {
                float normalized_des_v = user_input ? des_v / -max_v_construction : des_v / Math.Max(min_input_v, -max_v_construction);
                if (float.IsInfinity(normalized_des_v) || float.IsNaN(normalized_des_v))
                    normalized_des_v = 0.0f;
                normalized_des_v = Common.Clampf(normalized_des_v, 1.0f);
                float scaled_restrained_v = Math.Max(min_input_v, -max_v_construction);
                des_v = normalized_des_v * scaled_restrained_v;
            }

            return des_v + snapping_vel;
        }

        [AutoGuiAttr("quadr Kp", true, "G6")]
        float quadr_Kp = 0.4f;

        /// <summary>
        ///   A control coefficient for acceleration. Has units of (angle / time) / time^2.
        /// </summary>
        /// <remarks>
        ///   <para>
        ///     "Parabolic steepness of control, governed by control utility authority and craft
        ///     characteristics. Should be positive."
        ///   </para>
        ///   <para>
        ///     If the error in velocity is e, then this controller will attempt to correct the
        ///     error in an amount of time equal to sqrt(e / kacc_quadr).
        ///   </para>
        /// </remarks>
        [AutoGuiAttr("kacc_quadr", false, "G6")]
        float kacc_quadr;
        bool first_quadr = true;

        [AutoGuiAttr("kacc_smoothing", true, "G5")]
        float kacc_smoothing = 10.0f;

        [AutoGuiAttr("relaxation_k", true, "G5")]
        float relaxation_k = -1.0f;

        [AutoGuiAttr("relaxation_Kp", true, "G5")]
        float relaxation_Kp = 0.5f;

        [AutoGuiAttr("relaxation_frame", true)]
        int relaxation_frame = 1;

        [AutoGuiAttr("relaxation_frame", false)]
        int relax_count = 0;

        /// <summary>
        /// Given a desired velocity, calculate the best acceleration to use in order to attain
        /// that velocity.
        /// </summary>
        protected override float get_desired_acceleration(float desiredVelocity)
        {
            float new_kacc_quadr = 0.0f;
            if (AtmosphereAutopilot.AeroModel == AtmosphereAutopilot.AerodinamycsModel.FAR)
                new_kacc_quadr = (float)(quadr_Kp * (imodel.roll_rot_model_gen.A[0, 1] * imodel.roll_rot_model_gen.B[1, 0] +
                    imodel.roll_rot_model_gen.A[0, 2] * imodel.roll_rot_model_gen.B[2, 0] + imodel.roll_rot_model_gen.B[0, 0]));
            if (AtmosphereAutopilot.AeroModel == AtmosphereAutopilot.AerodinamycsModel.Stock)
                new_kacc_quadr = (float)(quadr_Kp * (imodel.roll_rot_model_gen.A[0, 1] * imodel.roll_rot_model_gen.C[1, 0] +
                    imodel.roll_rot_model_gen.A[0, 2] * imodel.roll_rot_model_gen.B[2, 0] + imodel.roll_rot_model_gen.B[0, 0]));
            new_kacc_quadr = Math.Abs(new_kacc_quadr);
            if (float.IsNaN(new_kacc_quadr))
                return base.get_desired_acceleration(desiredVelocity);
            if (first_quadr)
                kacc_quadr = new_kacc_quadr;
            else
                kacc_quadr = (float)Common.simple_filter(new_kacc_quadr, kacc_quadr, kacc_smoothing);
            if (kacc_quadr < 1e-3)
                return base.get_desired_acceleration(desiredVelocity);
            first_quadr = false;
            float v_error = vel - desiredVelocity;
            float desired_deriv;
            float deltaTime = TimeWarp.fixedDeltaTime;

            // quadr_x is apparently the negation of an estimate of the amount of time it will take
            // to attain the desired velocity.
            double quadr_x = -Math.Sqrt(Math.Abs(v_error) / kacc_quadr);
            if (quadr_x >= -relaxation_k * deltaTime)
            {
                if (++relax_count > relaxation_frame)
                {
                    float avg_vel = 0.0f;
                    for (int i = 0; i < relaxation_frame; i++)
                        avg_vel += imodel.AngularVelHistory(axis).getFromTail(i);
                    avg_vel /= (float)relaxation_frame;
                    v_error = avg_vel - desiredVelocity;
                    if (relax_count > relaxation_frame * 2)
                        relax_count--;
                }
                desired_deriv = (float)(relaxation_Kp * -v_error / (Math.Ceiling(relaxation_k) * deltaTime));
            }
            else
            {
                relax_count = 0;
                double leftover_dt = Math.Min(deltaTime, -quadr_x);
                if (double.IsNaN(v_error))
                    desired_deriv = 0.0f;
                else
                {
                    // The motivation behind this formula seems to be:
                    // "By what (negative) quantity would abs(v_error) have to be
                    // increased in order to increase quadr_x by leftover_dt?"
                    double absoluteVelocityAfterTick = 
                          kacc_quadr * Math.Pow(quadr_x + leftover_dt, 2.0)
                        - kacc_quadr * Math.Pow(quadr_x, 2.0);
                    double velocityAfterTick = Math.Sign(v_error) * absoluteVelocityAfterTick;
                    desired_deriv = (float)velocityAfterTick / deltaTime;
                }
            }

            return desired_deriv;
        }
    }
}
