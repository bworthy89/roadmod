using System;

namespace Game.Vehicles;

[Flags]
public enum TaxiFlags : uint
{
	Returning = 1u,
	Requested = 2u,
	Arriving = 4u,
	Boarding = 8u,
	Disembarking = 0x10u,
	Transporting = 0x20u,
	RequiresMaintenance = 0x40u,
	Dispatched = 0x80u,
	FromOutside = 0x100u,
	Disabled = 0x200u
}
