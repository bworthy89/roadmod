using System;

namespace Game.Prefabs;

[Flags]
public enum PrefabComponents : uint
{
	Locked = 1u,
	PlacedSignatureBuilding = 2u
}
