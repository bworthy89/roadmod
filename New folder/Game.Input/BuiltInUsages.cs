using System;

namespace Game.Input;

[Flags]
public enum BuiltInUsages
{
	Menu = 1,
	DefaultTool = 2,
	Overlay = 4,
	Tool = 8,
	CancelableTool = 0x10,
	Debug = 0x20,
	Editor = 0x40,
	PhotoMode = 0x80,
	Options = 0x100,
	Tutorial = 0x200,
	DiscardableTool = 0x400,
	All = 0x7FF,
	DefaultSet = 0x41E
}
