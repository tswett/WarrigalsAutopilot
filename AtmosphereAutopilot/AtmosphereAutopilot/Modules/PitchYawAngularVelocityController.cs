﻿/*
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
using UnityEngine;

namespace AtmosphereAutopilot
{
    public abstract class PitchYawAngularVelocityController : AngularVelAdaptiveController
    {
        protected PitchYawAngularVelocityController(Vessel vessel, string module_name,
            int wnd_id, int axis)
            : base(vessel, module_name, wnd_id, axis)
        {
            max_input_aoa = max_aoa * dgr2rad;
            min_input_aoa = -max_input_aoa;
            max_g_aoa = max_input_aoa;
            min_g_aoa = -max_g_aoa;
        }

        protected Matrix eq_A = new Matrix(2, 2);
        protected Matrix eq_b = new Matrix(2, 1);
        protected Matrix eq_x;

        [AutoGuiAttr("max_input_aoa", false, "G6")]
        public float max_input_aoa;

        [AutoGuiAttr("max_input_v", false, "G6")]
        public float max_input_v;

        [AutoGuiAttr("min_input_aoa", false, "G6")]
        public float min_input_aoa;

        [AutoGuiAttr("min_input_v", false, "G6")]
        public float min_input_v;

        [AutoGuiAttr("max_g_aoa", false, "G6")]
        public float max_g_aoa;

        [AutoGuiAttr("min_g_aoa", false, "G6")]
        protected float min_g_aoa;

        [AutoGuiAttr("max_g_v", false, "G6")]
        public float max_g_v;

        [AutoGuiAttr("min_g_v", false, "G6")]
        public float min_g_v;

        [AutoGuiAttr("max_aoa_v", false, "G6")]
        public float max_aoa_v;

        [AutoGuiAttr("min_aoa_v", false, "G6")]
        public float min_aoa_v;

        [AutoGuiAttr("staticaly_stable", false)]
        public bool staticaly_stable = true;

        [AutoGuiAttr("moder_filter", true, "G6")]
        protected float moder_filter = 3.0f;

        [AutoGuiAttr("transit_v_mult", true, "G6")]
        protected float transit_v_mult = 0.5f;

        [AutoGuiAttr("neutral_offset", false, "G6")]
        public float neutral_offset = 0.0f;

        [GlobalSerializable("moder_cutoff_ias")]
        [AutoGuiAttr("moder_cutoff_ias", true, "G4")]
        public float moder_cutoff_ias = 10.0f;

        protected Matrix state_mat = new Matrix(4, 1);
        protected Matrix input_mat = new Matrix(1, 1);

        protected LinearSystemModel lin_model;

        protected override float process_desired_v(float des_v, bool user_input)
        {
            float rad_max_aoa = max_aoa * dgr2rad;
            res_max_aoa = 100.0f;
            res_min_aoa = -100.0f;
            res_equilibr_v_upper = 0.0f;
            res_equilibr_v_lower = 0.0f;
            float cur_aoa = imodel.AoA(axis);
            float abs_cur_aoa = Math.Abs(cur_aoa);
            bool moderated = false;


            // AoA moderation section
            if (moderate_aoa && imodel.dyn_pressure > moder_cutoff_ias * moder_cutoff_ias)
            {
                moderated = true;

                if (abs_cur_aoa < rad_max_aoa * 1.5f)
                {
                    // We're in linear regime so we can update our limitations

                    // get equilibrium aoa and angular_v for 1.0 input
                    try
                    {
                        eq_A[0, 0] = lin_model.A[0, 0];
                        eq_A[0, 1] = lin_model.A[0, 1];
                        eq_A[1, 0] = lin_model.A[1, 0];
                        eq_A[1, 1] = 0.0;
                        eq_b[0, 0] = -(lin_model.A[0, 2] + lin_model.A[0, 3] + lin_model.B[0, 0] + lin_model.C[0, 0]);
                        eq_b[1, 0] = -(lin_model.A[1, 2] + lin_model.A[1, 3] + lin_model.B[1, 0] + lin_model.C[1, 0]);
                        eq_A.old_lu = true;
                        eq_x = eq_A.SolveWith(eq_b);
                        if (!double.IsInfinity(eq_x[0, 0]) && !double.IsNaN(eq_x[0, 0]))
                        {
                            if (eq_x[0, 0] < 0.0)
                            {
                                // plane is statically unstable, eq_x solution is equilibrium on it's minimal stable aoa
                                min_input_aoa = (float)Common.simple_filter(0.6 * eq_x[0, 0], min_input_aoa, moder_filter / 2.0);
                                min_input_v = (float)Common.simple_filter(0.6 * eq_x[1, 0], min_input_v, moder_filter / 2.0);
                                staticaly_stable = false;
                            }
                            else
                            {
                                // plane is statically stable, eq_x solution is equilibrium on it's maximal stable aoa
                                max_input_aoa = (float)Common.simple_filter(eq_x[0, 0], max_input_aoa, moder_filter);
                                max_input_v = (float)Common.simple_filter(eq_x[1, 0], max_input_v, moder_filter);
                                staticaly_stable = true;
                            }

                            // get equilibrium aoa and angular_v for -1.0 input
                            eq_b[0, 0] = -lin_model.C[0, 0] + lin_model.A[0, 2] + lin_model.A[0, 3] + lin_model.B[0, 0];
                            eq_b[1, 0] = lin_model.A[1, 2] + lin_model.A[1, 3] + lin_model.B[1, 0] - lin_model.C[1, 0];
                            eq_x = eq_A.SolveWith(eq_b);
                            if (!double.IsInfinity(eq_x[0, 0]) && !double.IsNaN(eq_x[0, 0]))
                            {
                                if (eq_x[0, 0] >= 0.0)
                                {
                                    // plane is statically unstable, eq_x solution is equilibrium on it's maximal stable aoa
                                    max_input_aoa = (float)Common.simple_filter(0.6 * eq_x[0, 0], max_input_aoa, moder_filter / 2.0);
                                    max_input_v = (float)Common.simple_filter(0.6 * eq_x[1, 0], max_input_v, moder_filter / 2.0);
                                }
                                else
                                {
                                    // plane is statically stable, eq_x solution is equilibrium on it's minimal stable aoa
                                    min_input_aoa = (float)Common.simple_filter(eq_x[0, 0], min_input_aoa, moder_filter);
                                    min_input_v = (float)Common.simple_filter(eq_x[1, 0], min_input_v, moder_filter);
                                }
                            }
                        }
                    }
                    catch (MSingularException) { }

                    // get equilibrium v for max_aoa
                    eq_A[0, 0] = lin_model.A[0, 1];
                    eq_A[0, 1] = lin_model.A[0, 2] + lin_model.A[0, 3] + lin_model.B[0, 0];
                    eq_A[1, 0] = lin_model.A[1, 1];
                    eq_A[1, 1] = lin_model.A[1, 2] + lin_model.A[1, 3] + lin_model.B[1, 0];
                    eq_b[0, 0] = -(lin_model.A[0, 0] * rad_max_aoa + lin_model.C[0, 0]);
                    eq_b[1, 0] = -(lin_model.A[1, 0] * rad_max_aoa + lin_model.C[1, 0]);
                    eq_A.old_lu = true;
                    try
                    {
                        eq_x = eq_A.SolveWith(eq_b);
                        double new_max_aoa_v = eq_x[0, 0];
                        eq_b[0, 0] = -(lin_model.A[0, 0] * -rad_max_aoa + lin_model.C[0, 0]);
                        eq_b[1, 0] = -(lin_model.A[1, 0] * -rad_max_aoa + lin_model.C[1, 0]);
                        eq_x = eq_A.SolveWith(eq_b);
                        double new_min_aoa_v = eq_x[0, 0];
                        if (!double.IsInfinity(new_max_aoa_v) && !double.IsNaN(new_max_aoa_v)
                            && !double.IsInfinity(new_min_aoa_v) && !double.IsNaN(new_min_aoa_v))
                        {
                            max_aoa_v = (float)Common.simple_filter(new_max_aoa_v, max_aoa_v, moder_filter);
                            min_aoa_v = (float)Common.simple_filter(new_min_aoa_v, min_aoa_v, moder_filter);
                        }
                    }
                    catch (MSingularException) { }
                }

                // let's apply moderation with controllability region
                if (max_input_aoa < res_max_aoa)
                {
                    res_max_aoa = max_input_aoa;
                    res_equilibr_v_upper = max_input_v;
                }
                if (min_input_aoa > res_min_aoa)
                {
                    res_min_aoa = min_input_aoa;
                    res_equilibr_v_lower = min_input_v;
                }

                // apply simple AoA moderation
                if (rad_max_aoa < res_max_aoa)
                {
                    res_max_aoa = rad_max_aoa;
                    res_equilibr_v_upper = max_aoa_v;
                }
                if (-rad_max_aoa > res_min_aoa)
                {
                    res_min_aoa = -rad_max_aoa;
                    res_equilibr_v_lower = min_aoa_v;
                }
            }

            // Lift acceleration moderation section
            if (moderate_g && imodel.dyn_pressure > moder_cutoff_ias * moder_cutoff_ias)
            {
                moderated = true;

                if (Math.Abs(lin_model.A[0, 0]) > 1e-5 && abs_cur_aoa < rad_max_aoa * 1.5f)
                {
                    // model may be sane, let's update limitations
                    double gravity_acc = 0.0;
                    switch (axis)
                    {
                        case PITCH:
                            gravity_acc = imodel.pitch_gravity_acc + imodel.pitch_noninert_acc;
                            break;
                        case YAW:
                            gravity_acc = imodel.yaw_gravity_acc + imodel.yaw_noninert_acc;
                            break;
                        default:
                            gravity_acc = 0.0;
                            break;
                    }
                    // get equilibrium aoa and angular v for max_g g-force
                    max_g_v = (float)Common.simple_filter(
                        (max_g_force * 9.81 + gravity_acc) / imodel.surface_v_magnitude,
                        max_g_v, moder_filter);
                    min_g_v = (float)Common.simple_filter(
                        (-max_g_force * 9.81 + gravity_acc) / imodel.surface_v_magnitude,
                        min_g_v, moder_filter);
                    // get equilibrium aoa for max_g
                    eq_A[0, 0] = lin_model.A[0, 0];
                    eq_A[0, 1] = lin_model.A[0, 2] + lin_model.A[0, 3] + lin_model.B[0, 0];
                    eq_A[1, 0] = lin_model.A[1, 0];
                    eq_A[1, 1] = lin_model.A[1, 2] + lin_model.A[1, 3] + lin_model.B[1, 0];
                    eq_b[0, 0] = -(max_g_v + lin_model.C[0, 0]);
                    eq_b[1, 0] = -lin_model.C[1, 0];
                    eq_A.old_lu = true;
                    try
                    {
                        eq_x = eq_A.SolveWith(eq_b);
                        double new_max_g_aoa = eq_x[0, 0];
                        eq_b[0, 0] = -(min_g_v + lin_model.C[0, 0]);
                        eq_x = eq_A.SolveWith(eq_b);
                        double new_min_g_aoa = eq_x[0, 0];
                        if (!double.IsInfinity(new_max_g_aoa) && !double.IsNaN(new_max_g_aoa) &&
                            !double.IsInfinity(new_min_g_aoa) && !double.IsNaN(new_min_g_aoa))
                        {
                            max_g_aoa = (float)Common.simple_filter(new_max_g_aoa, max_g_aoa, moder_filter);
                            min_g_aoa = (float)Common.simple_filter(new_min_g_aoa, min_g_aoa, moder_filter);
                        }
                    }
                    catch (MSingularException) { }
                }

                // apply moderation
                if (max_g_aoa < 2.0 && max_g_aoa > 0.0 && min_g_aoa > -2.0 && max_g_aoa > min_g_aoa)       // sanity check
                {
                    if (max_g_aoa < res_max_aoa)
                    {
                        res_max_aoa = max_g_aoa;
                        res_equilibr_v_upper = max_g_v;
                    }
                    if (min_g_aoa > res_min_aoa)
                    {
                        res_min_aoa = min_g_aoa;
                        res_equilibr_v_lower = min_g_v;
                    }
                }
            }

            // let's get non-overshooting max v value, let's call it transit_max_v
            // we start on 0.0 aoa with transit_max_v and we must not overshoot res_max_aoa
            // while applying -1.0 input all the time
            if (abs_cur_aoa < rad_max_aoa * 1.5f && moderated)
            {
                double transit_max_aoa = Math.Min(rad_max_aoa, res_max_aoa);
                state_mat[0, 0] = staticaly_stable ? transit_max_aoa / 3.0 : transit_max_aoa;
                state_mat[2, 0] = -1.0;
                state_mat[3, 0] = -1.0;
                input_mat[0, 0] = -1.0;
                double acc = lin_model.eval_row(1, state_mat, input_mat);
                float new_dyn_max_v = transit_v_mult * (float)Math.Sqrt(2.0 * transit_max_aoa * (-acc));
                if (float.IsNaN(new_dyn_max_v))
                {
                    if (old_dyn_max_v != 0.0f)
                        transit_max_v = old_dyn_max_v;
                    else
                        old_dyn_max_v = max_v_construction;
                }
                else
                {
                    // for cases when static authority is too small to comply to long-term dynamics, 
                    // we need to artificially increase it
                    if (new_dyn_max_v < res_equilibr_v_upper * 1.2 || new_dyn_max_v < -res_equilibr_v_lower * 1.2)
                        new_dyn_max_v = 1.2f * Math.Max(Math.Abs(res_equilibr_v_upper), Math.Abs(res_equilibr_v_lower));
                    new_dyn_max_v = Common.Clampf(new_dyn_max_v, max_v_construction);
                    transit_max_v = (float)Common.simple_filter(new_dyn_max_v, transit_max_v, moder_filter);
                    old_dyn_max_v = transit_max_v;
                }
            }
            else
                transit_max_v = max_v_construction;

            // if the user is in charge, let's hold surface-relative angular elocity
            float v_offset = 0.0f;
            if (user_input && vessel.obt_speed > 1.0)
            {
                if (FlightGlobals.speedDisplayMode == FlightGlobals.SpeedDisplayModes.Surface)
                {
                    Vector3 planet2vessel = vessel.GetWorldPos3D() - vessel.mainBody.position;
                    Vector3 still_ang_v = Vector3.Cross(vessel.obt_velocity, planet2vessel) / planet2vessel.sqrMagnitude;
                    Vector3 principal_still_ang_v = imodel.world_to_cntrl_part * still_ang_v;
                    v_offset = principal_still_ang_v[axis];
                }
            }
            if (user_input)
                v_offset += neutral_offset;

            // desired_v moderation section
            if (user_controlled)
            {
                float normalized_des_v = des_v / max_v_construction;
                if (float.IsInfinity(normalized_des_v) || float.IsNaN(normalized_des_v))
                    normalized_des_v = 0.0f;
                normalized_des_v = Common.Clampf(normalized_des_v, 1.0f);
                if (moderated)
                {
                    float max_v = Mathf.Min(max_v_construction, transit_max_v);
                    float min_v = -max_v;
                    // upper aoa limit moderation
                    scaled_aoa_up = Common.Clampf((res_max_aoa - cur_aoa) * 2.0f / (res_max_aoa - res_min_aoa), 1.0f);
                    if (scaled_aoa_up < 0.0f)
                        max_v = Mathf.Min(max_v, scaled_aoa_up * max_v + (1.0f + scaled_aoa_up) * Math.Min(res_equilibr_v_upper, max_v));
                    else
                        max_v = Mathf.Min(max_v, scaled_aoa_up * max_v + (1.0f - scaled_aoa_up) * Math.Min(res_equilibr_v_upper, max_v));
                    // lower aoa limit moderation
                    scaled_aoa_down = Common.Clampf((res_min_aoa - cur_aoa) * 2.0f / (res_min_aoa - res_max_aoa), 1.0f);
                    if (scaled_aoa_down < 0.0f)
                        min_v = Mathf.Max(min_v, scaled_aoa_down * min_v + (1.0f + scaled_aoa_down) * Math.Max(res_equilibr_v_lower, min_v));
                    else
                        min_v = Mathf.Max(min_v, scaled_aoa_down * min_v + (1.0f - scaled_aoa_down) * Math.Max(res_equilibr_v_lower, min_v));
                    // now let's restrain v
                    scaled_restrained_v = Common.Clampf(v_offset, min_v, max_v);
                    scaled_restrained_v = Mathf.Lerp(scaled_restrained_v, des_v >= 0.0 ? max_v : min_v, Mathf.Abs(normalized_des_v));
                }
                else
                    scaled_restrained_v = transit_max_v * normalized_des_v + v_offset;
                des_v = scaled_restrained_v;
            }
            return des_v;
        }

        [AutoGuiAttr("quadr Kp", true, "G6")]
        protected float quadr_Kp = 0.45f;

        [AutoGuiAttr("kacc_quadr", false, "G6")]
        internal float kacc_quadr;

        protected bool first_quadr = true;

        [AutoGuiAttr("kacc_smoothing", true, "G5")]
        protected float kacc_smoothing = 10.0f;

        [AutoGuiAttr("relaxation_k", true, "G5")]
        public float relaxation_k = -1.0f;

        [AutoGuiAttr("relaxation_Kp", true, "G5")]
        public float relaxation_Kp = 0.5f;

        [AutoGuiAttr("relaxation_avg_frame", true)]
        protected int relaxation_frame = 1;

        [AutoGuiAttr("relax_count", false)]
        protected int relax_count = 0;

        protected override float get_desired_acceleration(float des_v)
        {
            float new_kacc_quadr = 0.0f;
            if (AtmosphereAutopilot.AeroModel == AtmosphereAutopilot.AerodinamycsModel.FAR)
                new_kacc_quadr = (float)(quadr_Kp * (lin_model.A[1, 2] * lin_model.B[2, 0] + lin_model.A[1, 3] * lin_model.B[3, 0] + lin_model.B[1, 0]));
            if (AtmosphereAutopilot.AeroModel == AtmosphereAutopilot.AerodinamycsModel.Stock)
                new_kacc_quadr = (float)(quadr_Kp * (lin_model.A[1, 2] * lin_model.C[2, 0] + lin_model.A[1, 3] * lin_model.B[3, 0] + lin_model.B[1, 0]));
            new_kacc_quadr = Math.Abs(new_kacc_quadr);
            if (float.IsNaN(new_kacc_quadr) || float.IsInfinity(new_kacc_quadr))
                return base.get_desired_acceleration(des_v);
            if (first_quadr)
                kacc_quadr = new_kacc_quadr;
            else
                kacc_quadr = (float)Common.simple_filter(new_kacc_quadr, kacc_quadr, kacc_smoothing);
            if (kacc_quadr < 1e-5)
                return base.get_desired_acceleration(des_v);
            first_quadr = false;
            double v_error = vel - des_v;
            double quadr_x;
            float desired_deriv;
            float dt = TimeWarp.fixedDeltaTime;

            quadr_x = -Math.Sqrt(Math.Abs(v_error) / kacc_quadr);
            if (quadr_x >= -relaxation_k * dt)
            {
                if (++relax_count > relaxation_frame)
                {
                    float avg_vel = 0.0f;
                    for (int i = 0; i < relaxation_frame; i++)
                        avg_vel += imodel.AngularVelHistory(axis).getFromTail(i);
                    avg_vel /= (float)relaxation_frame;
                    v_error = avg_vel - des_v;
                    if (relax_count > relaxation_frame * 2)
                        relax_count--;
                }
                desired_deriv = (float)(relaxation_Kp * -v_error / (Math.Ceiling(relaxation_k) * dt));
            }
            else
            {
                relax_count = 0;
                double leftover_dt = Math.Min(dt, -quadr_x);
                if (double.IsNaN(v_error))
                    desired_deriv = 0.0f;
                else
                    desired_deriv = (float)(Math.Sign(v_error) * (kacc_quadr * Math.Pow(quadr_x + leftover_dt, 2.0) - kacc_quadr * quadr_x * quadr_x)) / dt;
            }

            return desired_deriv;
        }

        [AutoGuiAttr("transit_max_v", false, "G6")]
        public float transit_max_v;

        protected float old_dyn_max_v;

        [AutoGuiAttr("res_max_aoa", false, "G6")]
        public float res_max_aoa;

        [AutoGuiAttr("res_equolibr_v_upper", false, "G6")]
        public float res_equilibr_v_upper;

        [AutoGuiAttr("res_min_aoa", false, "G6")]
        public float res_min_aoa;

        [AutoGuiAttr("res_equolibr_v_lower", false, "G6")]
        public float res_equilibr_v_lower;

        [AutoGuiAttr("scaled_aoa_up", false, "G4")]
        protected float scaled_aoa_up;

        [AutoGuiAttr("scaled_aoa_down", false, "G4")]
        protected float scaled_aoa_down;

        [AutoGuiAttr("scaled_restr_v", false, "G6")]
        protected float scaled_restrained_v;

        [VesselSerializable("moderate_aoa")]
        [AutoGuiAttr("Moderate AoA", true, null)]
        public bool moderate_aoa = true;

        [VesselSerializable("moderate_g")]
        [AutoGuiAttr("Moderate G-force", true, null)]
        public bool moderate_g = true;

        [VesselSerializable("max_aoa")]
        [AutoGuiAttr("max AoA", true, "G6")]
        public float max_aoa = 15.0f;

        [VesselSerializable("max_g_force")]
        [AutoGuiAttr("max G-force", true, "G6")]
        public float max_g_force = 15.0f;
    }
}
