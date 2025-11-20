using System;

namespace Game.Vehicles;

[Flags]
public enum WatercraftLaneFlags : uint
{
	EndOfPath = 1u,
	EndReached = 2u,
	UpdateOptimalLane = 4u,
	TransformTarget = 8u,
	ResetSpeed = 0x10u,
	FixedStart = 0x20u,
	Obsolete = 0x40u,
	Reserved = 0x80u,
	FixedLane = 0x100u,
	NeedSignal = 0x200u,
	IgnoreSignal = 0x400u,
	GroupTarget = 0x800u,
	Queue = 0x1000u,
	IgnoreBlocker = 0x2000u,
	IsBlocked = 0x4000u,
	QueueReached = 0x8000u,
	Connection = 0x10000u,
	Area = 0x20000u,
	AlignLeft = 0x80000u,
	AlignRight = 0x100000u
}
