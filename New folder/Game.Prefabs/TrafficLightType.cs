using System;

namespace Game.Prefabs;

[Flags]
public enum TrafficLightType
{
	VehicleLeft = 1,
	VehicleRight = 2,
	CrossingLeft = 4,
	CrossingRight = 8,
	AllowFlipped = 0x10
}
