using System;

namespace Game.Creatures;

[Flags]
public enum PetFlags : uint
{
	None = 0u,
	Disembarking = 1u,
	Hangaround = 2u,
	Arrived = 4u,
	LeaderArrived = 8u
}
