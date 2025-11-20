using System;

namespace Game.Areas;

[Flags]
public enum AreaFlags : byte
{
	Complete = 1,
	CounterClockwise = 2,
	NoTriangles = 4,
	Slave = 8
}
