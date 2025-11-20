using System;

namespace Game.Prefabs;

[Flags]
public enum AgeMask : byte
{
	Child = 1,
	Teen = 2,
	Adult = 4,
	Elderly = 8,
	Any = 0xF
}
