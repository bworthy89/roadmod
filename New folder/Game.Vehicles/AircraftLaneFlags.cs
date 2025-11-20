using System;

namespace Game.Vehicles;

[Flags]
public enum AircraftLaneFlags : uint
{
	EndOfPath = 1u,
	EndReached = 2u,
	Connection = 4u,
	TransformTarget = 8u,
	ParkingSpace = 0x10u,
	ResetSpeed = 0x20u,
	Obsolete = 0x40u,
	Reserved = 0x80u,
	SkipLane = 0x100u,
	Checked = 0x400u,
	IgnoreBlocker = 0x2000u,
	Runway = 0x10000u,
	Airway = 0x20000u,
	Approaching = 0x100000u,
	Flying = 0x200000u,
	Landing = 0x400000u,
	TakingOff = 0x800000u
}
