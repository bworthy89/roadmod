using System;

namespace Game.Prefabs;

[Serializable]
public enum WaterLevelChangeType : byte
{
	None,
	Sine,
	RainControlled
}
