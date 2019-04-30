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
        float _minPitch = -75.0f;
        public override float MinOutput => _minPitch;
        float _maxPitch = 75.0f;
        public override float MaxOutput => _maxPitch;
        public override bool ReverseSense => true;

        public SpeedByPitchController(Vessel vessel, IPitchController pitchController)
        {
            Target = new EasTarget(vessel);
            ControlElement = new PitchElement(pitchController);
            SetPoint = 100.0f;
            CoeffP = 1.0f;
            TimeConstI = 20.0f;
        }

        protected override void DrawAdditionalControls()
        {
            DrawSlider($"Min pitch: {_minPitch}", ref _minPitch, -90.0f, 0.0f);
            DrawSlider($"Max pitch: {_maxPitch}", ref _maxPitch, 0.0f, 90.0f);
        }
    }
}
