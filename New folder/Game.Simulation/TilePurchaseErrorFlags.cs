using System;

namespace Game.Simulation;

[Flags]
public enum TilePurchaseErrorFlags
{
	None = 0,
	NoCurrentlyAvailable = 1,
	NoAvailable = 2,
	NoSelection = 4,
	InsufficientFunds = 8,
	InsufficientPermits = 0x10
}
