using System;

namespace Game.Pathfind;

[Flags]
public enum PathElementFlags : byte
{
	Secondary = 1,
	PathStart = 2,
	Action = 4,
	Return = 8,
	Reverse = 0x10,
	WaitPosition = 0x20,
	Leader = 0x40,
	Hangaround = 0x80
}
