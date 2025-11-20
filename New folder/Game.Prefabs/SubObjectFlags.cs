using System;

namespace Game.Prefabs;

[Flags]
public enum SubObjectFlags
{
	AnchorTop = 1,
	AnchorCenter = 2,
	RequireElevated = 0x10,
	RequireOutsideConnection = 0x20,
	RequireDeadEnd = 0x40,
	RequireOrphan = 0x80,
	OnGround = 0x100,
	WaterwayCrossing = 0x200,
	NotWaterwayCrossing = 0x400,
	EdgePlacement = 0x1000,
	MiddlePlacement = 0x2000,
	AllowCombine = 0x4000,
	CoursePlacement = 0x8000,
	FlipInverted = 0x10000,
	StartPlacement = 0x20000,
	EndPlacement = 0x40000,
	MakeOwner = 0x100000,
	OnMedian = 0x200000,
	FixedPlacement = 0x400000,
	PreserveShape = 0x800000,
	EvenSpacing = 0x1000000,
	SpacingOverride = 0x2000000,
	OnAttachedParent = 0x4000000
}
