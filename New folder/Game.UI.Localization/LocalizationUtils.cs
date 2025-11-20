using Game.Common;

namespace Game.UI.Localization;

public static class LocalizationUtils
{
	public static string AppendIndex(string localeId, RandomLocalizationIndex randomLocalizationIndex)
	{
		if (randomLocalizationIndex.m_Index == -1)
		{
			return localeId;
		}
		return $"{localeId}:{randomLocalizationIndex.m_Index}";
	}
}
