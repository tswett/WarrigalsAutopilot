using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarrigalsAutopilot.ControlElements;
using WarrigalsAutopilot.ControlTargets;

namespace WarrigalsAutopilot.Controllers
{
    public class HeadingController : PidController
    {
        public override string Name => "Heading hold";
        public override float SliderMaxCoeffP => 2.0f;
        float _maxBank = 30.0f;
        public override float MinOutput => -_maxBank;
        public override float MaxOutput => _maxBank;

        public HeadingController(Vessel vessel, IBankController bankController)
        {
            Target = new HeadingTarget(vessel);
            ControlElement = new BankElement(bankController);
            SetPoint = 90.0f;
            CoeffP = 1.0f;
            TimeConstI = 2.0f;
        }

        protected override void DrawAdditionalControls()
        {
            DrawSlider($"Max bank: {_maxBank}", ref _maxBank, 0.0f, 90.0f);
        }
    }
}
