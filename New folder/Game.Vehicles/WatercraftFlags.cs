using System;

namespace Game.Vehicles;

[Flags]
public enum WatercraftFlags : uint
{
	StayOnWaterway = 1u,
	AnyLaneTarget = 2u,
	Queueing = 4u,
	DeckLights = 8u,
	LightsOff = 0x10u,
	Working = 0x20u
}
