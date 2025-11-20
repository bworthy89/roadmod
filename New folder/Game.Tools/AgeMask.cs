using System;

namespace Game.Tools;

[Flags]
public enum AgeMask : byte
{
	Sapling = 1,
	Young = 2,
	Mature = 4,
	Elderly = 8
}
