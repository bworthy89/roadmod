using System;

namespace Game.Pathfind;

[Flags]
public enum PathfindFlags : ushort
{
	Stable = 1,
	IgnoreFlow = 2,
	ForceForward = 4,
	ForceBackward = 8,
	NoHeuristics = 0x10,
	ParkingReset = 0x20,
	Simplified = 0x40,
	MultipleOrigins = 0x80,
	MultipleDestinations = 0x100,
	IgnoreExtraStartAccessRequirements = 0x200,
	IgnoreExtraEndAccessRequirements = 0x400,
	IgnorePath = 0x800,
	SkipPathfind = 0x1000
}
