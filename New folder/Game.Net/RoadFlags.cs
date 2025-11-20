using System;

namespace Game.Net;

[Flags]
public enum RoadFlags : byte
{
	StartHalfAligned = 1,
	EndHalfAligned = 2,
	IsLit = 4,
	AlwaysLit = 8,
	LightsOff = 0x10
}
