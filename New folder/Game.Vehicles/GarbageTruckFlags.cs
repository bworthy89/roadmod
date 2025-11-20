using System;

namespace Game.Vehicles;

[Flags]
public enum GarbageTruckFlags : uint
{
	Returning = 1u,
	IndustrialWasteOnly = 2u,
	Unloading = 4u,
	Disabled = 8u,
	EstimatedFull = 0x10u,
	ClearChecked = 0x20u
}
