using System;

namespace Game.Simulation;

[Flags]
public enum ServiceRequestFlags : byte
{
	Reversed = 1,
	SkipCooldown = 2
}
