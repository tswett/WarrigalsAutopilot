using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WarrigalsAutopilot.ControlElements
{
    public class VertSpeedControlElement : ControlElement
    {
        public Controller VertSpeedController { get; private set; }

        public VertSpeedControlElement(Controller vertSpeedController)
        {
            VertSpeedController = vertSpeedController;
        }

        public override float MinOutput => -100.0f;
        public override float MaxOutput => 100.0f;

        public override void SetOutput(float output) => VertSpeedController.SetPoint = output;
    }
}
