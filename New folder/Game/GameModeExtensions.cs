namespace Game;

public static class GameModeExtensions
{
	public static string ToRichPresence(this GameMode gameMode)
	{
		return gameMode switch
		{
			GameMode.MainMenu => "#StatusInMainMenu", 
			GameMode.Game => "#StatusInGame", 
			GameMode.Editor => "#StatusInEditor", 
			_ => string.Empty, 
		};
	}

	public static bool IsEditor(this GameMode gameMode)
	{
		return (gameMode & GameMode.Editor) == GameMode.Editor;
	}

	public static bool IsGame(this GameMode gameMode)
	{
		return (gameMode & GameMode.Game) == GameMode.Game;
	}

	public static bool IsGameOrEditor(this GameMode gameMode)
	{
		return (gameMode & GameMode.GameOrEditor) != 0;
	}
}
