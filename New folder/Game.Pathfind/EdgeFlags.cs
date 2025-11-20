using System;

namespace Game.Pathfind;

[Flags]
public enum EdgeFlags : ushort
{
	Forward = 1,
	Backward = 2,
	AllowMiddle = 4,
	SingleOnly = 8,
	SecondaryStart = 0x10,
	SecondaryEnd = 0x20,
	FreeForward = 0x40,
	FreeBackward = 0x80,
	Secondary = 0x100,
	AllowEnter = 0x200,
	AllowExit = 0x400,
	RequireAuthorization = 0x4000,
	OutsideConnection = 0x8000,
	DefaultMask = 0xFEFF
}
