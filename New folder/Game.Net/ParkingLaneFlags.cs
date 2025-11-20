using System;

namespace Game.Net;

[Flags]
public enum ParkingLaneFlags
{
	Invert = 1,
	StartingLane = 2,
	EndingLane = 4,
	AdditionalStart = 8,
	ParkingInverted = 0x10,
	LeftSide = 0x20,
	RightSide = 0x40,
	TaxiAvailabilityUpdated = 0x80,
	TaxiAvailabilityChanged = 0x100,
	VirtualLane = 0x200,
	FindConnections = 0x400,
	ParkingLeft = 0x800,
	ParkingRight = 0x1000,
	ParkingDisabled = 0x2000,
	AllowEnter = 0x4000,
	AllowExit = 0x8000,
	SpecialVehicles = 0x10000,
	SecondaryStart = 0x20000
}
