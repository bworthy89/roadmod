using System;

namespace Game.Prefabs;

[Flags]
public enum TrainFlags : byte
{
	MultiUnit = 1,
	Pantograph = 2
}
