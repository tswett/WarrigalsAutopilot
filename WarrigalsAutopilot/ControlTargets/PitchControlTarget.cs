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
    public class PitchControlTarget : ControlTarget
    {
        Vessel _vessel;

        public PitchControlTarget(Vessel vessel)
        {
            _vessel = vessel;
        }

        public override string Name => "Pitch angle";
        public override float MinSetPoint => -90.0f;
        public override float MaxSetPoint => 90.0f;

        public override float ProcessVariable { get => _vessel.GetPitchAngle(); }
    }
}
