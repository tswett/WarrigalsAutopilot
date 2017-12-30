namespace WarrigalsAutopilot.ControlElements
{
    public class AileronControlElement : ControlElement
    {
        Vessel _vessel;

        public AileronControlElement(Vessel vessel)
        {
            _vessel = vessel;
        }

        public override float MinOutput => -1.0f;
        public override float MaxOutput => 1.0f;

        public override void SetOutput(float output) => _vessel.ctrlState.roll = output;

        public override void OnEnable() => Trim = _vessel.ctrlState.rollTrim;
        public override void OnDisable()
        {
            _vessel.ctrlState.rollTrim = Trim;
            if (_vessel = FlightGlobals.ActiveVessel)
            {
                FlightInputHandler.state.rollTrim = Trim;
            }
        }
    }
}