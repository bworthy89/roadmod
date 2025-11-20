using System;

namespace Game.Vehicles;

[Flags]
public enum CargoTransportFlags : uint
{
	Returning = 1u,
	EnRoute = 2u,
	Boarding = 4u,
	Arriving = 8u,
	RequiresMaintenance = 0x10u,
	Refueling = 0x20u,
	AbandonRoute = 0x40u,
	RouteSource = 0x80u,
	Testing = 0x100u,
	RequireStop = 0x200u,
	DummyTraffic = 0x400u,
	Disabled = 0x800u
}
