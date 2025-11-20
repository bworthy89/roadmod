using System;

namespace Game.Prefabs;

[Flags]
public enum SecondaryNetLaneFlags
{
	Left = 1,
	Right = 2,
	OneSided = 4,
	RequireSafe = 8,
	CanFlipSides = 0x10,
	RequireParallel = 0x20,
	RequireOpposite = 0x40,
	RequireSingle = 0x80,
	RequireMultiple = 0x100,
	RequireAllowPassing = 0x200,
	RequireForbidPassing = 0x400,
	RequireMerge = 0x800,
	RequireContinue = 0x1000,
	RequireStop = 0x2000,
	Crossing = 0x4000,
	RequireUnsafe = 0x8000,
	RequirePavement = 0x10000,
	RequireYield = 0x20000,
	DuplicateSides = 0x40000,
	RequireSafeMaster = 0x80000,
	RequireRoundabout = 0x100000,
	RequireNotRoundabout = 0x200000
}
