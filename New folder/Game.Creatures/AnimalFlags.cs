using System;

namespace Game.Creatures;

[Flags]
public enum AnimalFlags : uint
{
	Roaming = 1u,
	SwimmingTarget = 2u,
	FlyingTarget = 4u
}
