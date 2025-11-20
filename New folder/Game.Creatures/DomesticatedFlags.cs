using System;

namespace Game.Creatures;

[Flags]
public enum DomesticatedFlags : uint
{
	None = 0u,
	Idling = 1u,
	Wandering = 2u
}
