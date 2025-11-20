using System;

namespace Game.Triggers;

[Flags]
public enum TargetType
{
	Nothing = 0,
	Building = 1,
	Citizen = 2,
	Policy = 4,
	Road = 8,
	ServiceBuilding = 0x10
}
