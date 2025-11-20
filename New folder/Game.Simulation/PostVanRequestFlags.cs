using System;

namespace Game.Simulation;

[Flags]
public enum PostVanRequestFlags : byte
{
	Deliver = 1,
	Collect = 2,
	BuildingTarget = 4,
	MailBoxTarget = 8
}
