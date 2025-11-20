using System;

namespace Game.Buildings;

[Flags]
public enum BuildingFlags : byte
{
	None = 0,
	HighRentWarning = 1,
	StreetLightsOff = 2,
	LowEfficiency = 4,
	Illuminated = 8
}
