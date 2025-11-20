using System;

namespace Game.Prefabs;

[Flags]
public enum NetSectionFlags
{
	Invert = 1,
	Median = 2,
	Left = 4,
	Right = 8,
	Underground = 0x10,
	Overhead = 0x20,
	FlipLanes = 0x40,
	AlignCenter = 0x80,
	FlipMesh = 0x100,
	Hidden = 0x200,
	HiddenSurface = 0x400,
	HiddenSide = 0x800,
	HiddenTop = 0x1000,
	HiddenBottom = 0x2000,
	HalfLength = 0x4000
}
