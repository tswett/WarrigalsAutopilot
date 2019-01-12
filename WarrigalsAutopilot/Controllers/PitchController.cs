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
        public override float MinOutput => -10.0f;
        public override float MaxOutput => 15.0f;

        public PitchController(Vessel vessel, IAoAController aoaController)
        {
            Target = new PitchTarget(vessel);
            ControlElement = new AoAElement(aoaController);
            SetPoint = 5.0f;
            CoeffP = 1.0f;
            TimeConstI = 0.5f;
        }
    }
}
