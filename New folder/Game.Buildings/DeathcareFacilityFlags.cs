using System;

namespace Game.Buildings;

[Flags]
public enum DeathcareFacilityFlags : byte
{
	HasAvailableHearses = 1,
	HasRoomForBodies = 2,
	CanProcessCorpses = 4,
	CanStoreCorpses = 8,
	IsFull = 0x10
}
