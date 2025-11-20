using System;

namespace Game.Objects;

[Flags]
public enum TrafficLightState : ushort
{
	None = 0,
	Red = 1,
	Yellow = 2,
	Green = 4,
	Flashing = 8
}
