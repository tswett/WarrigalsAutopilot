using System;
using UnityEngine;

namespace WarrigalsAutopilot
{
    class BankControlTarget : ControlTarget
    {
        Vessel _vessel;

        public BankControlTarget(Vessel vessel)
        {
            _vessel = vessel;
        }

        public override string Name => "Bank angle";
        public override float ProcessVariable
        {
            get
            {
                Vector3 worldUp = (Vector3)_vessel.upAxis;
                Vector3 vesselRight = _vessel.transform.right;
                // transform.forward is down, not forward (and transform.up is forward, not up)
                Vector3 vesselUp = -_vessel.transform.forward;

                float y = Vector3.Dot(worldUp, vesselRight);
                float x = Vector3.Dot(worldUp, vesselUp);

                float rawBank = -Mathf.Atan2(y, x) * 180 / Mathf.PI;

                //Debug.Log(
                //    $"WAP: worldUp: {worldUp}, vesselRight: {vesselRight}, vesselUp: {vesselUp}, " +
                //    $"y: {y}, x: {x}, rawBank: {rawBank}");

                return AngleSubtract(rawBank, 0.0f);
            }
        }

        public override float ErrorFromSetPoint(float setPoint)
        {
            return AngleSubtract(ProcessVariable, setPoint);
        }
    }
}
