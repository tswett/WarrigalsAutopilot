using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarrigalsAutopilot.ControlElements;
using WarrigalsAutopilot.ControlTargets;

namespace WarrigalsAutopilot.Controllers
{
    public class AltitudeController : PidController
    {
        public override string Name => "Altitude hold";
        public override float SliderMaxCoeffP => 1.0f;
        float _maxVertSpeed = 50.0f;
        public override float MinOutput => -_maxVertSpeed;
        public override float MaxOutput => _maxVertSpeed;

        public AltitudeController(Vessel vessel, IVertSpeedController vertSpeedController)
        {
            Target = new AltitudeTarget(vessel);
            ControlElement = new VertSpeedElement(vertSpeedController);
            SetPoint = 2000.0f;
            CoeffP = 0.5f;
            TimeConstI = 10.0f;
            UseCoeffI = false;
        }

        protected override void DrawAdditionalControls()
        {
            DrawSlider($"Max vert speed: {_maxVertSpeed}", ref _maxVertSpeed, 0.0f, 500.0f);
        }
    }
}
