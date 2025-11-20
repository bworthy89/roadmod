using cohtml.Net;
using Colossal.Localization;

namespace Game.UI.Localization;

public class UILocalizationManager : ILocalizationManager
{
	private readonly LocalizationManager m_LocalizationManager;

	public UILocalizationManager(LocalizationManager localizationManager)
	{
		m_LocalizationManager = localizationManager;
	}

	private UILocalizationManager()
	{
	}

	public override void Translate(string key, TranslationData data)
	{
		if (m_LocalizationManager != null && m_LocalizationManager.activeDictionary.TryGetValue(key, out var value))
		{
			data.Set(value);
		}
		else
		{
			data.Set(key);
		}
	}
}
