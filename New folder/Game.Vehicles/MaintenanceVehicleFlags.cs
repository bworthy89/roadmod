using System;

namespace Game.Vehicles;

[Flags]
public enum MaintenanceVehicleFlags : uint
{
	Returning = 1u,
	TransformTarget = 2u,
	EdgeTarget = 4u,
	TryWork = 8u,
	Working = 0x10u,
	ClearingDebris = 0x20u,
	Full = 0x40u,
	EstimatedFull = 0x80u,
	Disabled = 0x100u,
	ClearChecked = 0x200u
}
