using System;

namespace Game.Vehicles;

[Flags]
public enum TrainLaneFlags : uint
{
	EndOfPath = 1u,
	EndReached = 2u,
	Return = 4u,
	PushBlockers = 8u,
	ResetSpeed = 0x10u,
	HighBeams = 0x20u,
	Obsolete = 0x40u,
	Reserved = 0x80u,
	TurnLeft = 0x100u,
	TurnRight = 0x200u,
	BlockReserve = 0x400u,
	ParkingSpace = 0x800u,
	KeepClear = 0x4000u,
	TryReserve = 0x8000u,
	Connection = 0x10000u,
	Exclusive = 0x20000u,
	FullReserve = 0x40000u
}
