using System;

namespace Game.Vehicles;

[Flags]
public enum PersonalCarFlags : uint
{
	Transporting = 1u,
	Boarding = 2u,
	Disembarking = 4u,
	DummyTraffic = 8u,
	HomeTarget = 0x10u
}
