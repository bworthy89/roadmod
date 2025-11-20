using System;

namespace Game.Prefabs;

[Flags]
public enum MeshFlags : uint
{
	Decal = 1u,
	StackX = 2u,
	StackY = 4u,
	StackZ = 8u,
	Impostor = 0x10u,
	Tiling = 0x20u,
	Invert = 0x40u,
	Base = 0x80u,
	MinBounds = 0x100u,
	Default = 0x200u,
	Animated = 0x1000u,
	Skeleton = 0x2000u,
	Character = 0x4000u,
	Prop = 0x8000u
}
