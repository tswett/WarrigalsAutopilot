using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WarrigalsAutopilot.ControlElements
{
    public class PitchControlElement : ControlElement
    {
        public Controller PitchController { get; private set; }

        public PitchControlElement(Controller pitchController)
        {
            PitchController = pitchController;
        }

        public override float MinOutput => -45.0f;
        public override float MaxOutput => 15.0f;

        public override void SetOutput(float output) => PitchController.SetPoint = output;
    }
}
