using System;

namespace Game.Creatures;

[Flags]
public enum WildlifeFlags : uint
{
	None = 0u,
	Idling = 1u,
	Wandering = 2u
}
