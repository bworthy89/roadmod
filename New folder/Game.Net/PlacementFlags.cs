using System;

namespace Game.Net;

[Flags]
public enum PlacementFlags
{
	None = 0,
	OnGround = 1,
	Floating = 2,
	IsUpgrade = 4,
	UpgradeOnly = 8,
	AllowParallel = 0x10,
	NodeUpgrade = 0x20,
	FlowLeft = 0x40,
	FlowRight = 0x80,
	UndergroundUpgrade = 0x100,
	LinkAuxOffsets = 0x200,
	ShoreLine = 0x400
}
