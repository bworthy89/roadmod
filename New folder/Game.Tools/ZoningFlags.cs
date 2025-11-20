using System;

namespace Game.Tools;

[Flags]
public enum ZoningFlags : uint
{
	FloodFill = 1u,
	Marquee = 2u,
	Zone = 4u,
	Dezone = 8u,
	Paint = 0x10u,
	Overwrite = 0x20u
}
