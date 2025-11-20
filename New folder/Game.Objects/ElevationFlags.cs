using System;

namespace Game.Objects;

[Flags]
public enum ElevationFlags : byte
{
	Stacked = 1,
	OnGround = 2,
	Lowered = 4,
	OnAttachedParent = 8
}
