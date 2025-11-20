using System;

namespace Game.Areas;

[Flags]
public enum GeometryFlags
{
	PhysicalGeometry = 1,
	CanOverrideObjects = 2,
	ProtectedArea = 4,
	ClearArea = 8,
	ClipTerrain = 0x10,
	ShiftTerrain = 0x20,
	OnWaterSurface = 0x40,
	PseudoRandom = 0x80,
	RequireWater = 0x100,
	HiddenIngame = 0x200,
	SubAreaBatch = 0x400
}
