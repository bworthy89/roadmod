using System;

namespace Game.Prefabs;

[Flags]
public enum ActivityCondition : uint
{
	Homeless = 1u,
	Angry = 2u,
	Sad = 4u,
	Happy = 8u,
	Waiting = 0x10u,
	Collapsed = 0x20u
}
