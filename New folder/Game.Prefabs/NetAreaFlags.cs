using System;

namespace Game.Prefabs;

[Flags]
public enum NetAreaFlags
{
	Buildable = 1,
	Invert = 2,
	Hole = 4,
	NoBridge = 8,
	Median = 0x10
}
