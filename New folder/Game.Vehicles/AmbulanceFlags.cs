using System;

namespace Game.Vehicles;

[Flags]
public enum AmbulanceFlags : uint
{
	Returning = 1u,
	Dispatched = 2u,
	Transporting = 4u,
	AnyHospital = 8u,
	FindHospital = 0x10u,
	AtTarget = 0x20u,
	Disembarking = 0x40u,
	Disabled = 0x80u,
	Critical = 0x100u
}
