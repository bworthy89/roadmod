using System;

namespace Game.UI.Widgets;

[Flags]
public enum ListOperations
{
	None = 0,
	AddElement = 1,
	Clear = 2,
	MoveUp = 2,
	MoveDown = 4,
	Duplicate = 8,
	Delete = 0x10
}
