using System;

namespace Game.Vehicles;

[Flags]
public enum FireEngineFlags : uint
{
	Returning = 1u,
	Extinguishing = 2u,
	Empty = 4u,
	DisasterResponse = 8u,
	Rescueing = 0x10u,
	EstimatedEmpty = 0x20u,
	Disabled = 0x40u
}
