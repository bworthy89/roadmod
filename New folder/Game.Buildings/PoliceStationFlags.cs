using System;

namespace Game.Buildings;

[Flags]
public enum PoliceStationFlags : byte
{
	HasAvailablePatrolCars = 1,
	HasAvailablePoliceHelicopters = 2,
	NeedPrisonerTransport = 4
}
