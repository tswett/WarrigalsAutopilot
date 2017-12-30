namespace WarrigalsAutopilot.ControlElements
{
    public class AileronControlElement : ControlElement
    {
        Vessel _vessel;

        public AileronControlElement(Vessel vessel)
        {
            _vessel = vessel;
        }

        public override float Trim
        {
            get => _vessel.ctrlState.rollTrim;
            set
            {
                _vessel.ctrlState.rollTrim = value;
                if (_vessel = FlightGlobals.ActiveVessel)
                {
                    FlightInputHandler.state.rollTrim = value;
                }
            }
        }

        public override float MinOutput => -1.0f;
        public override float MaxOutput => 1.0f;

        public override void SetOutput(float output) => _vessel.ctrlState.roll = output;
    }
}