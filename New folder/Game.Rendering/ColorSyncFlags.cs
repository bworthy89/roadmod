using System;

namespace Game.Rendering;

[Flags]
public enum ColorSyncFlags : byte
{
	None = 0,
	SameGroup = 1,
	SameIndex = 2,
	DifferentGroup = 4,
	DifferentIndex = 8,
	SyncRangeVariation = 0x10
}
