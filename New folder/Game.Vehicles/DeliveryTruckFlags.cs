using System;

namespace Game.Vehicles;

[Flags]
public enum DeliveryTruckFlags : uint
{
	Returning = 1u,
	Loaded = 2u,
	DummyTraffic = 4u,
	Buying = 0x10u,
	StorageTransfer = 0x20u,
	Delivering = 0x40u,
	UpkeepDelivery = 0x80u,
	TransactionCancelled = 0x100u,
	UpdateOwnerQuantity = 0x200u,
	UpdateSellerQuantity = 0x400u
}
