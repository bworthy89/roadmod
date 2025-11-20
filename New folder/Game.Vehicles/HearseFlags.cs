using System;

namespace Game.Vehicles;

[Flags]
public enum HearseFlags : uint
{
	Returning = 1u,
	Dispatched = 2u,
	Transporting = 4u,
	AtTarget = 8u,
	Disembarking = 0x10u,
	Disabled = 0x20u
}
