using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarrigalsAutopilot.ControlElements;
using WarrigalsAutopilot.ControlTargets;

namespace WarrigalsAutopilot.Controllers
{
    public interface IBankController : IController { }

    public class BankController : PidController, IBankController
    {
        public override string Name => "Wing leveler";

        public BankController(Vessel vessel)
        {
            Target = new BankTarget(vessel);
            ControlElement = new AileronElement(vessel);
            SetPoint = 0.0f;
            CoeffP = 0.01f;
            TimeConstI = 2.0f;
        }
    }
}
