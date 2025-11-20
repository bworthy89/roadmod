using System;

namespace Game.Citizens;

[Flags]
public enum HouseholdFlags : byte
{
	None = 0,
	Tourist = 1,
	Commuter = 2,
	MovedIn = 4
}
