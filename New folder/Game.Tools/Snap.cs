using System;

namespace Game.Tools;

[Flags]
public enum Snap : uint
{
	ExistingGeometry = 1u,
	CellLength = 2u,
	StraightDirection = 4u,
	NetSide = 8u,
	NetArea = 0x10u,
	OwnerSide = 0x20u,
	ObjectSide = 0x40u,
	NetMiddle = 0x80u,
	Shoreline = 0x100u,
	NearbyGeometry = 0x200u,
	GuideLines = 0x400u,
	ZoneGrid = 0x800u,
	NetNode = 0x1000u,
	ObjectSurface = 0x2000u,
	Upright = 0x4000u,
	LotGrid = 0x8000u,
	AutoParent = 0x10000u,
	PrefabType = 0x20000u,
	ContourLines = 0x40000u,
	Distance = 0x80000u,
	None = 0u,
	All = uint.MaxValue
}
