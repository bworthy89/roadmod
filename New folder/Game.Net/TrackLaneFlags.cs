using System;

namespace Game.Net;

[Flags]
public enum TrackLaneFlags
{
	Invert = 1,
	Twoway = 2,
	Switch = 4,
	DiamondCrossing = 8,
	Exclusive = 0x10,
	LevelCrossing = 0x20,
	AllowMiddle = 0x40,
	CrossingTraffic = 0x80,
	MergingTraffic = 0x100,
	StartingLane = 0x200,
	EndingLane = 0x400,
	Station = 0x800,
	TurnLeft = 0x1000,
	TurnRight = 0x2000,
	DoubleSwitch = 0x4000
}
