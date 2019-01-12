using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarrigalsAutopilot.ControlElements;
using WarrigalsAutopilot.ControlTargets;

namespace WarrigalsAutopilot.Controllers
{
    public interface IAoAController : IController { }

    public class AoAController : PidController, IAoAController
    {
        public override string Name => "Angle of attack";

        public AoAController(Vessel vessel)
        {
            Target = new AoATarget(vessel);
            ControlElement = new ElevatorElement(vessel);
            SetPoint = 0.0f;
            CoeffP = 0.01f;
            TimeConstI = 1.0f;
        }
    }
}
