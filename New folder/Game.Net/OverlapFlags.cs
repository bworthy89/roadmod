using System;

namespace Game.Net;

[Flags]
public enum OverlapFlags : ushort
{
	MergeStart = 1,
	MergeEnd = 2,
	OverlapLeft = 4,
	OverlapRight = 8,
	MergeMiddleStart = 0x10,
	MergeMiddleEnd = 0x20,
	Unsafe = 0x40,
	Road = 0x80,
	Track = 0x100,
	MergeFlip = 0x200,
	Slow = 0x400,
	Water = 0x800
}
