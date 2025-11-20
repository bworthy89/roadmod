using System;

namespace Game.Creatures;

[Flags]
public enum AnimalTravelFlags : uint
{
	None = 0u,
	Flying = 1u,
	Swimming = 2u
}
