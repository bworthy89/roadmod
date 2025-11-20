using System;

namespace Game.Prefabs;

[Flags]
public enum LaneFlags
{
	Invert = 1,
	Slave = 2,
	Master = 4,
	Road = 8,
	Pedestrian = 0x10,
	Parking = 0x20,
	Track = 0x40,
	Twoway = 0x80,
	DisconnectedStart = 0x100,
	DisconnectedEnd = 0x200,
	Secondary = 0x400,
	Utility = 0x800,
	Underground = 0x1000,
	CrossRoad = 0x2000,
	PublicOnly = 0x4000,
	OnWater = 0x8000,
	Virtual = 0x10000,
	FindAnchor = 0x20000,
	LeftLimit = 0x40000,
	RightLimit = 0x80000,
	ParkingLeft = 0x100000,
	ParkingRight = 0x200000,
	HasAuxiliary = 0x400000,
	EvenSpacing = 0x800000,
	PseudoRandom = 0x1000000,
	BicyclesOnly = 0x2000000,
	TrackFlow = 0x4000000
}
