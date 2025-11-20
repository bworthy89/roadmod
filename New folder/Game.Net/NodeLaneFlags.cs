using System;

namespace Game.Net;

[Flags]
public enum NodeLaneFlags : byte
{
	StartWidthOffset = 1,
	EndWidthOffset = 2,
	StartBicycleOnly = 4,
	EndBicycleOnly = 8
}
