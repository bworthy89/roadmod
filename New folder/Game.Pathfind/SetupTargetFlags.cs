using System;

namespace Game.Pathfind;

[Flags]
public enum SetupTargetFlags : uint
{
	None = 0u,
	Industrial = 1u,
	Commercial = 2u,
	Import = 4u,
	Service = 8u,
	Residential = 0x10u,
	Export = 0x20u,
	SecondaryPath = 0x40u,
	RequireTransport = 0x80u,
	PathEnd = 0x100u,
	BuildingUpkeep = 0x200u
}
