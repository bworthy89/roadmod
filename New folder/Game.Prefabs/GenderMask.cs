using System;

namespace Game.Prefabs;

[Flags]
public enum GenderMask : byte
{
	Female = 1,
	Male = 2,
	Other = 4,
	Any = 7
}
