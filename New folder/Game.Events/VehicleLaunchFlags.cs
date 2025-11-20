using System;

namespace Game.Events;

[Flags]
public enum VehicleLaunchFlags : uint
{
	PathRequested = 1u,
	Launched = 2u
}
