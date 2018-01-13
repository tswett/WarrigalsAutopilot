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

using System;
using UnityEngine;

namespace WarrigalsAutopilot
{
    static class VesselExtensions
    {
        public static Vector3 GetWorldUp(this Vessel vessel) => vessel.upAxis;

        public static Vector3 GetVesselRight(this Vessel vessel) => vessel.transform.right;
        // transform.forward is down, not forward (and transform.up is forward, not up)
        public static Vector3 GetVesselUp(this Vessel vessel) => -vessel.transform.forward;
        // transform.up is forward, not up
        public static Vector3 GetVesselForward(this Vessel vessel) => vessel.transform.up;

        public static Vector3 GetCelestialNorth(this Vessel vessel) => vessel.mainBody.transform.up;

        public static Vector3 GetEast(this Vessel vessel) =>
            Vector3.Cross(vessel.GetWorldUp(), vessel.GetCelestialNorth()).normalized;

        public static Vector3 GetNorth(this Vessel vessel) =>
            Vector3.Cross(vessel.GetEast(), vessel.GetWorldUp());

        public static float GetBankAngle(this Vessel vessel)
        {
            float y = Vector3.Dot(vessel.GetWorldUp(), vessel.GetVesselRight());
            float x = Vector3.Dot(vessel.GetWorldUp(), vessel.GetVesselUp());

            float rawBank = -Mathf.Atan2(y, x) * 180 / Mathf.PI;

            return AngleSubtract(rawBank, 0.0f);
        }

        public static float GetPitchAngle(this Vessel vessel) =>
            90 - Vector3.Angle(vessel.GetWorldUp(), vessel.GetVesselForward());

        public static float GetHeading(this Vessel vessel)
        {
            float y = Vector3.Dot(vessel.GetVesselForward(), vessel.GetEast());
            float x = Vector3.Dot(vessel.GetVesselForward(), vessel.GetNorth());

            float rawHeading = Mathf.Atan2(y, x) * 180 / Mathf.PI;

            return AngleSubtract(rawHeading, 180.0f) + 180.0f;
        }

        internal static float AngleSubtract(float angle, float minusAngle)
        {
            float result = (angle - minusAngle) % 360.0f;

            if (result <= -180) result += 360;
            if (result > 180) result -= 360;

            return result;
        }
    }
}
