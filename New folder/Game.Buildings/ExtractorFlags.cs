using System;

namespace Game.Buildings;

[Flags]
public enum ExtractorFlags : byte
{
	Rotating = 1,
	Working = 2
}
