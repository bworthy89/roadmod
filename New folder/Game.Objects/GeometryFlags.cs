using System;

namespace Game.Objects;

[Flags]
public enum GeometryFlags
{
	None = 0,
	Circular = 1,
	Overridable = 2,
	Marker = 4,
	ExclusiveGround = 8,
	DeleteOverridden = 0x10,
	Physical = 0x20,
	WalkThrough = 0x40,
	Standing = 0x80,
	CircularLeg = 0x100,
	OverrideZone = 0x200,
	OccupyZone = 0x400,
	CanSubmerge = 0x800,
	BaseCollision = 0x1000,
	IgnoreSecondaryCollision = 0x2000,
	OptionalAttach = 0x4000,
	Brushable = 0x8000,
	Stampable = 0x10000,
	LowCollisionPriority = 0x20000,
	IgnoreBottomCollision = 0x40000,
	HasBase = 0x80000,
	HasLot = 0x100000,
	IgnoreLegCollision = 0x200000
}
