using System;

namespace Game.Prefabs;

[Flags]
public enum NetPieceFlags
{
	PreserveShape = 1,
	BlockTraffic = 2,
	BlockCrosswalk = 4,
	Surface = 8,
	DisableTiling = 0x10,
	LowerBottomToTerrain = 0x20,
	AsymmetricMeshX = 0x40,
	AsymmetricMeshZ = 0x80,
	HasMesh = 0x100,
	Side = 0x200,
	RaiseTopToTerrain = 0x400,
	SmoothTopNormal = 0x800,
	SkipBottomHalf = 0x1000,
	Top = 0x2000,
	Bottom = 0x4000,
	HasRoadLanes = 0x8000,
	HasBicycleLanes = 0x10000
}
