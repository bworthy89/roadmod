using System;

namespace Game.Buildings;

[Flags]
public enum EmergencyShelterFlags : byte
{
	HasAvailableVehicles = 1,
	HasShelterSpace = 2
}
