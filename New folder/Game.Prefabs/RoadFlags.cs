using System;

namespace Game.Prefabs;

[Flags]
public enum RoadFlags
{
	EnableZoning = 1,
	SeparatedCarriageways = 2,
	PreferTrafficLights = 4,
	DefaultIsForward = 8,
	UseHighwayRules = 0x10,
	DefaultIsBackward = 0x20,
	HasStreetLights = 0x40
}
