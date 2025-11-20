using System;

namespace Game.Common;

[Flags]
public enum TypeMask : uint
{
	Terrain = 1u,
	StaticObjects = 2u,
	MovingObjects = 4u,
	Net = 8u,
	Zones = 0x10u,
	Areas = 0x20u,
	RouteWaypoints = 0x40u,
	RouteSegments = 0x80u,
	Labels = 0x100u,
	Water = 0x200u,
	Icons = 0x400u,
	WaterSources = 0x800u,
	Lanes = 0x1000u,
	None = 0u,
	All = uint.MaxValue
}
