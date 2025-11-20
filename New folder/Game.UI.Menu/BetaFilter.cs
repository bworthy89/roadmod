using System.Collections.Generic;

namespace Game.UI.Menu;

public static class BetaFilter
{
	private static HashSet<string> s_Options;

	public static IReadOnlyCollection<string> options => s_Options;

	public static void AddOption(string option)
	{
		s_Options.Add(option);
	}

	public static void AddOptions(params string[] options)
	{
		s_Options.UnionWith(options);
	}

	static BetaFilter()
	{
		s_Options = new HashSet<string>();
		AddOptions("Input.Editor", "Modding");
	}
}
