namespace Game.Net;

public enum TrafficLightState : byte
{
	None,
	Beginning,
	Ongoing,
	Ending,
	Changing,
	Extending,
	Extended
}
