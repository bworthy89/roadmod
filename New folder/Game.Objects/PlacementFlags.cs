using System;

namespace Game.Objects;

[Flags]
public enum PlacementFlags
{
	None = 0,
	RoadSide = 1,
	OnGround = 2,
	OwnerSide = 4,
	CanOverlap = 8,
	Shoreline = 0x10,
	Floating = 0x20,
	Hovering = 0x40,
	HasUndergroundElements = 0x80,
	RoadNode = 0x100,
	Unique = 0x200,
	Wall = 0x400,
	Hanging = 0x800,
	NetObject = 0x1000,
	RoadEdge = 0x2000,
	Swaying = 0x4000,
	HasProbability = 0x8000,
	Underwater = 0x10000,
	Waterway = 0x20000,
	SubNetSnap = 0x40000,
	Attached = 0x80000,
	RequirePedestrian = 0x100000
}
