using System;

namespace Game.Net;

[Flags]
public enum TrafficLightFlags : byte
{
	LevelCrossing = 1,
	MoveableBridge = 2,
	IsSubNode = 4
}
