using System;
using UnityEngine;

namespace WarrigalsAutopilot
{
    public class PitchControlTarget : ControlTarget
    {
        Vessel _vessel;

        public PitchControlTarget(Vessel vessel)
        {
            _vessel = vessel;
        }

        public override string Name => "Pitch angle";
        public override float MinSetPoint => -90.0f;
        public override float MaxSetPoint => 90.0f;

        public override float ProcessVariable
        {
            get
            {
                Vector3 worldUp = (Vector3)_vessel.upAxis;
                // transform.up is forward, not up
                Vector3 vesselForward = _vessel.transform.up;

                return 90 - Vector3.Angle(worldUp, vesselForward);
            }
        }
    }
}
