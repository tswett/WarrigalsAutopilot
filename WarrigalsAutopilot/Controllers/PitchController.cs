using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarrigalsAutopilot.ControlElements;
using WarrigalsAutopilot.ControlTargets;

namespace WarrigalsAutopilot.Controllers
{
    public interface IPitchController : IController { }

    public class PitchController : PidController, IPitchController
    {
        public override string Name => "Pitch control";
        public override float SliderMaxCoeffP => 5.0f;
        float _maxElevator = 0.5f;
        public override float MaxOutput => _maxElevator;

        public PitchController(Vessel vessel)
        {
            Target = new PitchTarget(vessel);
            ControlElement = new ElevatorElement(vessel);
            SetPoint = 5.0f;
            CoeffP = 0.02f;
            TimeConstI = 1.0f;
        }

        protected override void DrawAdditionalControls()
        {
            DrawSlider($"Max up elevator: {_maxElevator}", ref _maxElevator, 0.0f, 1.0f);
        }
    }
}
