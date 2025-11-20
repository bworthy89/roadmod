using System;

namespace Game.Tutorials;

[Serializable]
[Flags]
public enum PolicyAdjustmentTriggerTargetFlags
{
	City = 1,
	District = 2,
	Object = 4
}
