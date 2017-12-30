namespace WarrigalsAutopilot.ControlElements
{
    public class ElevatorControlElement : ControlElement
    {
        Vessel _vessel;

        public ElevatorControlElement(Vessel vessel)
        {
            _vessel = vessel;
        }

        public override float Trim
        {
            get => _vessel.ctrlState.pitchTrim;
            set
            {
                _vessel.ctrlState.pitchTrim = value;
                if (_vessel = FlightGlobals.ActiveVessel)
                {
                    FlightInputHandler.state.pitchTrim = value;
                }
            }
        }

        public override float MinOutput => -1.0f;
        public override float MaxOutput => 1.0f;

        public override void SetOutput(float output) => _vessel.ctrlState.pitch = output;
    }
}