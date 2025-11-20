using System;

namespace Game.Buildings;

[Flags]
public enum PrisonFlags : byte
{
	HasAvailablePrisonVans = 1,
	HasPrisonerSpace = 2
}
