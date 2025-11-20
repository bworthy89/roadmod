using System;

namespace Game.Prefabs;

[Flags]
public enum SubMeshFlags : uint
{
	RequireSafe = 1u,
	RequireLevelCrossing = 2u,
	RequireChild = 4u,
	RequireTeen = 8u,
	RequireAdult = 0x10u,
	RequireElderly = 0x20u,
	RequireDead = 0x40u,
	RequireStump = 0x80u,
	RequireEmpty = 0x100u,
	RequireFull = 0x200u,
	RequireEditor = 0x400u,
	RequireClear = 0x800u,
	RequireTrack = 0x1000u,
	IsStackStart = 0x2000u,
	IsStackMiddle = 0x4000u,
	IsStackEnd = 0x8000u,
	RequireLeftHandTraffic = 0x10000u,
	RequireRightHandTraffic = 0x20000u,
	DefaultMissingMesh = 0x40000u,
	RequirePartial1 = 0x80000u,
	RequirePartial2 = 0x100000u,
	HasTransform = 0x200000u,
	RequireForward = 0x400000u,
	RequireBackward = 0x800000u,
	OutlineOnly = 0x1000000u
}
