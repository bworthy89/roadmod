using System;

namespace Game.Buildings;

[Flags]
public enum TransportDepotFlags : byte
{
	HasAvailableVehicles = 1,
	HasDispatchCenter = 2
}
