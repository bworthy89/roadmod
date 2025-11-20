using System;

namespace Game.Net;

[Flags]
public enum SlaveLaneFlags
{
	AllowChange = 1,
	StartingLane = 2,
	EndingLane = 4,
	MultipleLanes = 8,
	MergingLane = 0x10,
	OpenStartLeft = 0x20,
	OpenStartRight = 0x40,
	OpenEndLeft = 0x80,
	OpenEndRight = 0x100,
	SplitLeft = 0x200,
	SplitRight = 0x400,
	MiddleStart = 0x800,
	MiddleEnd = 0x1000,
	MergeLeft = 0x2000,
	MergeRight = 0x4000
}
