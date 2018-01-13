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
    public class VertSpeedElement : Element
    {
        public Controller VertSpeedController { get; private set; }

        public VertSpeedElement(Controller vertSpeedController)
        {
            VertSpeedController = vertSpeedController;
        }

        public override float MinOutput => -100.0f;
        public override float MaxOutput => 100.0f;

        public override void SetOutput(float output) => VertSpeedController.SetPoint = output;
    }
}
