using System;

namespace Game.Prefabs;

[Flags]
public enum ObjectRequirementFlags : ushort
{
	Renter = 1,
	Children = 2,
	Snow = 4,
	Teens = 8,
	GoodWealth = 0x10,
	Dogs = 0x20,
	Homeless = 0x40
}
