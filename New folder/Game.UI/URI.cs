using System.Text.RegularExpressions;
using Unity.Entities;

namespace Game.UI;

public static class URI
{
	private static readonly Regex kEntityPattern = new Regex("^entity:\\/\\/(\\d+)\\/(\\d+)$", RegexOptions.Compiled);

	private static readonly Regex kInfoviewPattern = new Regex("^infoview:\\/\\/(\\d+)\\/(\\d+)$", RegexOptions.Compiled);

	public static string FromEntity(Entity entity)
	{
		return $"entity://{entity.Index}/{entity.Version}";
	}

	public static bool TryParseEntity(string input, out Entity entity)
	{
		Match match = kEntityPattern.Match(input);
		if (match.Success)
		{
			entity = new Entity
			{
				Index = int.Parse(match.Groups[1].Value),
				Version = int.Parse(match.Groups[2].Value)
			};
			return true;
		}
		entity = Entity.Null;
		return false;
	}

	public static string FromInfoView(Entity entity)
	{
		return $"infoview://{entity.Index}/{entity.Version}";
	}

	public static bool TryParseInfoview(string input, out Entity entity)
	{
		Match match = kInfoviewPattern.Match(input);
		if (match.Success)
		{
			entity = new Entity
			{
				Index = int.Parse(match.Groups[1].Value),
				Version = int.Parse(match.Groups[2].Value)
			};
			return true;
		}
		entity = Entity.Null;
		return false;
	}
}
