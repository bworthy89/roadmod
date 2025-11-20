using System;

namespace Game.Prefabs;

[Flags]
public enum PolicePurpose
{
	Patrol = 1,
	Emergency = 2,
	Intelligence = 4
}
