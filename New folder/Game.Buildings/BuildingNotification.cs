using System;

namespace Game.Buildings;

[Flags]
public enum BuildingNotification : byte
{
	None = 0,
	AirPollution = 1,
	NoisePollution = 2,
	GroundPollution = 4
}
