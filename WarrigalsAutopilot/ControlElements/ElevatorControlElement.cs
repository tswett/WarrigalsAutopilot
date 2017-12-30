namespace WarrigalsAutopilot.ControlElements
{
    public class ElevatorControlElement : ControlElement
    {
        Vessel _vessel;

        public ElevatorControlElement(Vessel vessel)
        {
            _vessel = vessel;
        }

        public override float MinOutput => -1.0f;
        public override float MaxOutput => 1.0f;

        public override void SetOutput(float output) => _vessel.ctrlState.pitch = output;

        public override void OnEnable() => Trim = _vessel.ctrlState.pitchTrim;
        public override void OnDisable()
        {
            _vessel.ctrlState.pitchTrim = Trim;
            if (_vessel = FlightGlobals.ActiveVessel)
            {
                FlightInputHandler.state.pitchTrim = Trim;
            }
        }
    }
}