using System;

namespace Game.Net;

[Flags]
public enum RoadTypes : byte
{
	None = 0,
	Car = 1,
	Watercraft = 2,
	Helicopter = 4,
	Airplane = 8,
	Bicycle = 0x10
}
