using System;

namespace Game.Vehicles;

[Flags]
public enum AircraftFlags : uint
{
	StayOnTaxiway = 1u,
	Emergency = 2u,
	StayMidAir = 4u,
	Blocking = 8u,
	Working = 0x10u,
	IgnoreParkedVehicle = 0x20u
}
