using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace WarrigalsAutopilot.ControlElements
{
    public class BankControlElement : ControlElement
    {
        public Controller BankController { get; private set; }

        public BankControlElement(Controller bankController)
        {
            BankController = bankController;
        }

        public override float MinOutput => -60.0f;
        public override float MaxOutput => 60.0f;

        public override void SetOutput(float output) => BankController.SetPoint = output;
    }
}
