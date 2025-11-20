using System;

namespace Game.Notifications;

[Flags]
public enum IconFlags : byte
{
	Unique = 1,
	IgnoreTarget = 2,
	TargetLocation = 4,
	OnTop = 8,
	SecondaryLocation = 0x10,
	CustomLocation = 0x20
}
