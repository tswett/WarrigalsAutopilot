// Copyright 2017 by Tanner "Warrigal" Swett.

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

namespace WarrigalsAutopilot.ControlTargets
{
    public class BankControlTarget : ControlTarget
    {
        Vessel _vessel;

        public BankControlTarget(Vessel vessel)
        {
            _vessel = vessel;
        }

        public override string Name => "Bank angle";
        public override float MinSetPoint => -180.0f;
        public override float MaxSetPoint => 180.0f;

        public override float ProcessVariable
        {
            get
            {
                Vector3 worldUp = (Vector3)_vessel.upAxis;
                Vector3 vesselRight = _vessel.transform.right;
                // transform.forward is down, not forward (and transform.up is forward, not up)
                Vector3 vesselUp = -_vessel.transform.forward;

                float y = Vector3.Dot(worldUp, vesselRight);
                float x = Vector3.Dot(worldUp, vesselUp);

                float rawBank = -Mathf.Atan2(y, x) * 180 / Mathf.PI;

                //Debug.Log(
                //    $"WAP: worldUp: {worldUp}, vesselRight: {vesselRight}, vesselUp: {vesselUp}, " +
                //    $"y: {y}, x: {x}, rawBank: {rawBank}");

                return AngleSubtract(rawBank, 0.0f);
            }
        }

        public override float ErrorFromSetPoint(float setPoint)
        {
            return AngleSubtract(ProcessVariable, setPoint);
        }
    }
}
