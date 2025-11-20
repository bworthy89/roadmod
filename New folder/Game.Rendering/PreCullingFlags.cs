using System;

namespace Game.Rendering;

[Flags]
public enum PreCullingFlags : uint
{
	PassedCulling = 1u,
	NearCamera = 2u,
	NearCameraUpdated = 4u,
	Deleted = 8u,
	Updated = 0x10u,
	Created = 0x20u,
	Applied = 0x40u,
	BatchesUpdated = 0x80u,
	Temp = 0x100u,
	FadeContainer = 0x200u,
	Object = 0x400u,
	Net = 0x800u,
	Lane = 0x1000u,
	Zone = 0x2000u,
	InfoviewColor = 0x4000u,
	BuildingState = 0x8000u,
	TreeGrowth = 0x10000u,
	LaneCondition = 0x20000u,
	InterpolatedTransform = 0x40000u,
	Animated = 0x80000u,
	Skeleton = 0x100000u,
	Emissive = 0x200000u,
	VehicleLayout = 0x400000u,
	EffectInstances = 0x800000u,
	Relative = 0x1000000u,
	SurfaceState = 0x2000000u,
	SurfaceDamage = 0x4000000u,
	ColorsUpdated = 0x8000000u,
	SmoothColor = 0x10000000u
}
