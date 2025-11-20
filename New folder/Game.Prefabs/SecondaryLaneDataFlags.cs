using System;

namespace Game.Prefabs;

[Flags]
public enum SecondaryLaneDataFlags
{
	SkipSafePedestrianOverlap = 1,
	SkipSafeCarOverlap = 2,
	SkipUnsafeCarOverlap = 4,
	SkipMergeOverlap = 8,
	FitToParkingSpaces = 0x10,
	SkipTrackOverlap = 0x20,
	EvenSpacing = 0x40,
	SkipSideCarOverlap = 0x80,
	InvertOverlapCuts = 0x100
}
