using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarrigalsAutopilot.ControlElements;
using WarrigalsAutopilot.ControlTargets;

namespace WarrigalsAutopilot.Controllers
{
    public class SpeedByPitchController : PidController
    {
        public override string Name => "Airspeed (by pitch)";
        public override float SliderMaxCoeffP => 10.0f;
        public override float MinOutput => -75.0f;
        public override float MaxOutput => 75.0f;
        public override bool ReverseSense => true;

        public SpeedByPitchController(Vessel vessel, IPitchController pitchController)
        {
            Target = new EasTarget(vessel);
            ControlElement = new PitchElement(pitchController);
            SetPoint = 100.0f;
            CoeffP = 1.0f;
            TimeConstI = 20.0f;
        }
    }
}
