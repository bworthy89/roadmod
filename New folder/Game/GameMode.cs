using System;

namespace Game;

[Flags]
public enum GameMode
{
	None = 0,
	Other = 1,
	Game = 2,
	Editor = 4,
	MainMenu = 8,
	GameOrEditor = 6,
	All = 0xF
}
