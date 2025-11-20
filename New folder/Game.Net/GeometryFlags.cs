using System;

namespace Game.Net;

[Flags]
public enum GeometryFlags
{
	StraightEdges = 1,
	StrictNodes = 2,
	SnapCellSize = 4,
	SupportRoundabout = 8,
	LoweredIsTunnel = 0x10,
	RaisedIsElevated = 0x20,
	NoEdgeConnection = 0x40,
	SnapToNetAreas = 0x80,
	StraightEnds = 0x100,
	RequireElevated = 0x200,
	SymmetricalEdges = 0x400,
	BlockZone = 0x800,
	Directional = 0x1000,
	SmoothSlopes = 0x2000,
	SmoothElevation = 0x4000,
	FlipTrafficHandedness = 0x8000,
	Asymmetric = 0x10000,
	FlattenTerrain = 0x20000,
	ClipTerrain = 0x40000,
	Marker = 0x80000,
	MiddlePillars = 0x100000,
	StandingNodes = 0x200000,
	ExclusiveGround = 0x400000,
	NoCurveSplit = 0x800000,
	SubOwner = 0x1000000,
	OnWater = 0x2000000,
	IsLefthanded = 0x4000000,
	InvertCompositionHandedness = 0x8000000,
	FlipCompositionHandedness = 0x10000000,
	ElevatedIsRaised = 0x20000000
}
