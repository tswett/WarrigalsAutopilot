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

namespace WarrigalsAutopilot.ControlTargets
{
    public abstract class Target
    {
        public abstract string Name { get; }

        public abstract float MinSetPoint { get; }
        public abstract float MaxSetPoint { get; }
        public abstract int MinSetPointInt { get; }
        public abstract int MaxSetPointInt { get; }
        public abstract bool WrapAround { get; }

        public abstract float ProcessVariable { get; }

        public virtual float ErrorFromSetPoint(float setPoint)
        {
            return ProcessVariable - setPoint;
        }
    }
}
