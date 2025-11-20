using System;

namespace Game.Simulation;

[Flags]
public enum GoodsDeliveryFlags : ushort
{
	BuildingUpkeep = 1,
	CommercialAllowed = 2,
	IndustrialAllowed = 4,
	ImportAllowed = 8,
	ResourceExportTarget = 0x10
}
