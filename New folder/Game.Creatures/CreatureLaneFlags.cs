using System;

namespace Game.Creatures;

[Flags]
public enum CreatureLaneFlags : uint
{
	EndOfPath = 1u,
	EndReached = 2u,
	TransformTarget = 4u,
	ParkingSpace = 8u,
	Obsolete = 0x10u,
	Transport = 0x20u,
	Connection = 0x40u,
	Taxi = 0x80u,
	Backward = 0x100u,
	WaitSignal = 0x200u,
	FindLane = 0x400u,
	Stuck = 0x800u,
	Area = 0x1000u,
	Hangaround = 0x2000u,
	Checked = 0x4000u,
	Action = 0x8000u,
	ActivityDone = 0x10000u,
	Swimming = 0x20000u,
	Flying = 0x40000u,
	WaitPosition = 0x80000u,
	Leader = 0x100000u,
	EmergeUnspawned = 0x200000u
}
