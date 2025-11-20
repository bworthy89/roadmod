using System;

namespace Game.Prefabs;

[Flags]
public enum LocalConnectFlags : uint
{
	ExplicitNodes = 1u,
	KeepOpen = 2u,
	RequireDeadend = 4u,
	ChooseBest = 8u,
	ChooseSides = 0x10u
}
