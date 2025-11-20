using System;

namespace Game.Objects;

[Flags]
public enum TreeState : byte
{
	Teen = 1,
	Adult = 2,
	Elderly = 4,
	Dead = 8,
	Stump = 0x10,
	Collected = 0x20
}
