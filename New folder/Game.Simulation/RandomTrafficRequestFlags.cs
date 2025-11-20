using System;

namespace Game.Simulation;

[Flags]
public enum RandomTrafficRequestFlags : byte
{
	NoSlowVehicles = 1,
	DeliveryTruck = 2,
	TransportVehicle = 4
}
