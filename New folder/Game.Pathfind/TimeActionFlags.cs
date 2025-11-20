using System;

namespace Game.Pathfind;

[Flags]
public enum TimeActionFlags
{
	SetPrimary = 1,
	SetSecondary = 2,
	EnableForward = 4,
	EnableBackward = 8
}
