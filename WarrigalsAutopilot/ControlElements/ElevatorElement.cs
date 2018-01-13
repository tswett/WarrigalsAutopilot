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

namespace WarrigalsAutopilot.ControlElements
{
    public class ElevatorElement : Element
    {
        Vessel _vessel;

        public ElevatorElement(Vessel vessel)
        {
            _vessel = vessel;
        }

        public override float Trim
        {
            get => _vessel.ctrlState.pitchTrim;
            set
            {
                _vessel.ctrlState.pitchTrim = value;
                if (_vessel = FlightGlobals.ActiveVessel)
                {
                    FlightInputHandler.state.pitchTrim = value;
                }
            }
        }

        public override float MinOutput => -1.0f;
        public override float MaxOutput => 1.0f;

        public override void SetOutput(float output) => _vessel.ctrlState.pitch = output;
    }
}