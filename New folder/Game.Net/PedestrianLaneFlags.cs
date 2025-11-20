using System;

namespace Game.Net;

[Flags]
public enum PedestrianLaneFlags
{
	Unsafe = 1,
	Crosswalk = 2,
	AllowMiddle = 4,
	AllowEnter = 8,
	SideConnection = 0x10,
	ForbidTransitTraffic = 0x20,
	OnWater = 0x40,
	AllowExit = 0x80,
	AllowBicycle = 0x100
}
