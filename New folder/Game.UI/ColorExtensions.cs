using UnityEngine;

namespace Game.UI;

public static class ColorExtensions
{
	public static string ToHexCode(this Color color, bool ignoreAlpha = false)
	{
		if (!ignoreAlpha)
		{
			return $"#{(int)(color.r * 255f):X2}{(int)(color.g * 255f):X2}{(int)(color.b * 255f):X2}{(int)(color.a * 255f):X2}";
		}
		return $"#{(int)(color.r * 255f):X2}{(int)(color.g * 255f):X2}{(int)(color.b * 255f):X2}";
	}
}
