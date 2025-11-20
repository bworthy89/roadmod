using System;

namespace Game.Pathfind;

[Flags]
public enum RuleFlags : byte
{
	HasBlockage = 1,
	ForbidCombustionEngines = 2,
	ForbidTransitTraffic = 4,
	ForbidHeavyTraffic = 8,
	ForbidPrivateTraffic = 0x10,
	ForbidSlowTraffic = 0x20,
	AvoidBicycles = 0x40
}
