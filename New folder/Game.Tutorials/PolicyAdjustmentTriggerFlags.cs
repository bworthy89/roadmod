using System;

namespace Game.Tutorials;

[Serializable]
[Flags]
public enum PolicyAdjustmentTriggerFlags
{
	Activated = 1,
	Deactivated = 2,
	Adjusted = 4
}
