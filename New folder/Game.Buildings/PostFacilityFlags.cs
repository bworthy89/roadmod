using System;

namespace Game.Buildings;

[Flags]
public enum PostFacilityFlags : byte
{
	CanDeliverMailWithVan = 1,
	CanCollectMailWithVan = 2,
	HasAvailableTrucks = 4,
	AcceptsUnsortedMail = 8,
	DeliversLocalMail = 0x10,
	AcceptsLocalMail = 0x20,
	DeliversUnsortedMail = 0x40
}
