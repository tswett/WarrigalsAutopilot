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

        public PitchController(Vessel vessel)
        {
            Target = new PitchTarget(vessel);
            ControlElement = new ElevatorElement(vessel);
            SetPoint = 5.0f;
            CoeffP = 0.02f;
            TimeConstI = 1.0f;
        }
    }
}
