﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using WarrigalsAutopilot.ControlElements;
using WarrigalsAutopilot.ControlTargets;

namespace WarrigalsAutopilot.Controllers
{
    public interface IVertSpeedController : IController { }

    public class VertSpeedController : PidController, IVertSpeedController
    {
        public override string Name => "Vertical speed";
        public override float SliderMaxCoeffP => 10.0f;
        float _minPitch = -45.0f;
        public override float MinOutput => _minPitch;
        float _maxPitch = 15.0f;
        public override float MaxOutput => _maxPitch;

        public VertSpeedController(Vessel vessel, IPitchController pitchController)
        {
            Target = new VertSpeedTarget(vessel);
            ControlElement = new PitchElement(pitchController);
            SetPoint = 0.0f;
            CoeffP = 1.0f;
            TimeConstI = 10.0f;
        }

        protected override void DrawAdditionalControls()
        {
            DrawSlider($"Min pitch: {_minPitch}", ref _minPitch, -90.0f, 0.0f);
            DrawSlider($"Max pitch: {_maxPitch}", ref _maxPitch, -90.0f, 0.0f);
        }
    }
}
