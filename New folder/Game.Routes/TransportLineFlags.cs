using System;

namespace Game.Routes;

[Flags]
public enum TransportLineFlags : ushort
{
	RequireVehicles = 1,
	NotEnoughVehicles = 2
}
