using System;

namespace Game.Events;

[Flags]
public enum DangerFlags : uint
{
	StayIndoors = 1u,
	Evacuate = 2u,
	UseTransport = 4u,
	WaitingCitizens = 8u
}
