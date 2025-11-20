using System;

namespace Game.Simulation.Flow;

[Flags]
public enum CutElementFlags
{
	None = 0,
	Created = 1,
	Admissible = 2,
	Changed = 4,
	Deleted = 8
}
