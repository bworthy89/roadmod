using System;

namespace Game.Creatures;

[Flags]
public enum ResidentFlags : uint
{
	None = 0u,
	Disembarking = 1u,
	ActivityDone = 2u,
	WaitingTransport = 4u,
	Arrived = 8u,
	Hangaround = 0x10u,
	InVehicle = 0x20u,
	PreferredLeader = 0x40u,
	NoLateDeparture = 0x80u,
	IgnoreTaxi = 0x100u,
	IgnoreTransport = 0x200u,
	IgnoreBenches = 0x400u,
	IgnoreAreas = 0x800u,
	CannotIgnore = 0x1000u,
	DummyTraffic = 0x2000u
}
