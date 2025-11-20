using System;

namespace Game.UI.Widgets;

[Flags]
public enum WidgetChanges : byte
{
	None = 0,
	Path = 1,
	Properties = 2,
	Children = 4,
	Visibility = 8,
	Activity = 0x10,
	TotalProperties = 0x1A
}
