using System;

namespace Game.Common;

[Flags]
public enum RaycastFlags : uint
{
	DebugDisable = 1u,
	UIDisable = 2u,
	ToolDisable = 4u,
	FreeCameraDisable = 8u,
	ElevateOffset = 0x10u,
	SubElements = 0x20u,
	Placeholders = 0x40u,
	Markers = 0x80u,
	NoMainElements = 0x100u,
	UpgradeIsMain = 0x200u,
	OutsideConnections = 0x400u,
	Outside = 0x800u,
	Cargo = 0x1000u,
	Passenger = 0x2000u,
	Decals = 0x4000u,
	EditorContainers = 0x8000u,
	SubBuildings = 0x10000u,
	PartialSurface = 0x20000u,
	BuildingLots = 0x40000u,
	IgnoreSecondary = 0x80000u
}
