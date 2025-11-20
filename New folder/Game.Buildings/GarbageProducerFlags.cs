using System;

namespace Game.Buildings;

[Flags]
public enum GarbageProducerFlags : byte
{
	None = 0,
	GarbagePilingUpWarning = 1
}
