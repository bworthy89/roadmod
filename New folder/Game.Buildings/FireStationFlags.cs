using System;

namespace Game.Buildings;

[Flags]
public enum FireStationFlags : byte
{
	HasAvailableFireEngines = 1,
	HasFreeFireEngines = 2,
	HasAvailableFireHelicopters = 4,
	HasFreeFireHelicopters = 8,
	DisasterResponseAvailable = 0x10
}
