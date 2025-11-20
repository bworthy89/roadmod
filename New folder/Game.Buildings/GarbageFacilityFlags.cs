using System;

namespace Game.Buildings;

[Flags]
public enum GarbageFacilityFlags : byte
{
	HasAvailableGarbageTrucks = 1,
	HasAvailableSpace = 2,
	IndustrialWasteOnly = 4,
	IsFull = 8,
	HasAvailableDeliveryTrucks = 0x10
}
