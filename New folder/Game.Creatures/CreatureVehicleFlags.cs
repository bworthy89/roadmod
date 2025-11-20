using System;

namespace Game.Creatures;

[Flags]
public enum CreatureVehicleFlags : uint
{
	Ready = 1u,
	Leader = 2u,
	Driver = 4u,
	Entering = 8u,
	Exiting = 0x10u
}
