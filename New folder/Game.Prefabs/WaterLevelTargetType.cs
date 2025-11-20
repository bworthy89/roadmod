using System;

namespace Game.Prefabs;

[Serializable]
public enum WaterLevelTargetType : byte
{
	None = 0,
	River = 1,
	Sea = 2,
	All = byte.MaxValue
}
