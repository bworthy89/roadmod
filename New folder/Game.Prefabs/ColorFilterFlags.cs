using System;

namespace Game.Prefabs;

[Flags]
public enum ColorFilterFlags : byte
{
	SeasonFilter = 1,
	BlendColor = 2,
	BlendProbability = 4
}
