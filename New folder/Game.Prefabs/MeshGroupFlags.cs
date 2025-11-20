using System;

namespace Game.Prefabs;

[Flags]
public enum MeshGroupFlags : uint
{
	RequireCold = 1u,
	RequireWarm = 2u,
	RequireHome = 4u,
	RequireHomeless = 8u,
	RequireMotorcycle = 0x10u,
	ForbidMotorcycle = 0x20u,
	RequireFishing = 0x40u,
	ForbidFishing = 0x80u,
	RequireBicycle = 0x100u,
	ForbidBicycle = 0x200u
}
