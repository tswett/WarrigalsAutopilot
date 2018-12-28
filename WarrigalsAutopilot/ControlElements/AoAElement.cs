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

namespace WarrigalsAutopilot.ControlElements
{
    public class AoAElement : Element
    {
        public Controller AoAController { get; private set; }

        public AoAElement(Controller aoaController)
        {
            UnityEngine.Debug.Log($"aoaController.Target is {aoaController.Target}");
            if (!(aoaController.Target is ControlTargets.AoATarget))
            {
                string message = "Tried to create a AoAElement out of a Controller whose target " +
                                 "is not a AoATarget.";
                throw new ArgumentOutOfRangeException(
                    nameof(aoaController), aoaController, message);
            }

            AoAController = aoaController;
        }

        public override float MinOutput => -180.0f;
        public override float MaxOutput => 180.0f;

        public override void SetOutput(float output) => AoAController.SetPoint = output;
    }
}
