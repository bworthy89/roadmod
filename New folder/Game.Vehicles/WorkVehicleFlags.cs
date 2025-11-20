using System;

namespace Game.Vehicles;

[Flags]
public enum WorkVehicleFlags : uint
{
	Returning = 1u,
	ExtractorVehicle = 2u,
	StorageVehicle = 4u,
	RouteSource = 8u,
	Arriving = 0x10u,
	WorkLocation = 0x20u,
	CargoMoveVehicle = 0x40u
}
