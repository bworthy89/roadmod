using System;

namespace Game.Vehicles;

[Flags]
public enum PostVanFlags : uint
{
	Returning = 1u,
	Delivering = 2u,
	Collecting = 4u,
	DeliveryEmpty = 8u,
	CollectFull = 0x10u,
	EstimatedEmpty = 0x20u,
	EstimatedFull = 0x40u,
	Disabled = 0x80u,
	ClearChecked = 0x100u
}
