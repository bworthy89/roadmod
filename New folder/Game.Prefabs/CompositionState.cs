using System;

namespace Game.Prefabs;

[Flags]
public enum CompositionState
{
	BlockUTurn = 1,
	ExclusiveGround = 2,
	HasSurface = 4,
	HasForwardRoadLanes = 8,
	SeparatedCarriageways = 0x10,
	HasPedestrianLanes = 0x20,
	HasBackwardRoadLanes = 0x40,
	HasForwardTrackLanes = 0x80,
	HasBackwardTrackLanes = 0x100,
	Asymmetric = 0x200,
	Marker = 0x400,
	BlockZone = 0x800,
	Multilane = 0x1000,
	LowerToTerrain = 0x2000,
	RaiseToTerrain = 0x4000,
	NoSubCollisions = 0x8000,
	Airspace = 0x10000,
	HalfLength = 0x20000,
	Hidden = 0x40000
}
