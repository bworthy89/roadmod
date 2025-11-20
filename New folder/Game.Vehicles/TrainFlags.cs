using System;

namespace Game.Vehicles;

[Flags]
public enum TrainFlags : uint
{
	Reversed = 1u,
	BoardingLeft = 2u,
	BoardingRight = 4u,
	Pantograph = 8u,
	IgnoreParkedVehicle = 0x10u
}
