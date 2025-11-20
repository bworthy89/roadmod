using System;

namespace Game.Rendering;

[Flags]
public enum DecalLayers
{
	Terrain = 1,
	Roads = 2,
	Buildings = 4,
	Vehicles = 8,
	Creatures = 0x10,
	Other = 0x20
}
