using System;
using Colossal.Localization;
using Colossal.UI.Binding;
using Game.Settings;

namespace Game.UI.Localization;

public class LocalizationBindings : CompositeBinding, IDisposable
{
	public enum DebugMode
	{
		None,
		Id,
		Fallback
	}

	private const string kGroup = "l10n";

	private readonly LocalizationManager m_LocalizationManager;

	private readonly GetterValueBinding<string[]> m_LocalesBinding;

	private readonly ValueBinding<int> m_DebugModeBinding;

	private readonly EventBinding m_ActiveDictionaryChangedBinding;

	private readonly RawMapBinding<string> m_IndexCountsBinding;

	public DebugMode debugMode
	{
		get
		{
			return (DebugMode)m_DebugModeBinding.value;
		}
		set
		{
			m_DebugModeBinding.Update((int)value);
		}
	}

	public LocalizationBindings(LocalizationManager localizationManager)
	{
		m_LocalizationManager = localizationManager;
		AddBinding(m_LocalesBinding = new GetterValueBinding<string[]>("l10n", "locales", () => m_LocalizationManager.GetSupportedLocales(), new ArrayWriter<string>(new StringWriter())));
		AddBinding(m_DebugModeBinding = new ValueBinding<int>("l10n", "debugMode", 0));
		AddBinding(m_ActiveDictionaryChangedBinding = new EventBinding("l10n", "activeDictionaryChanged"));
		AddBinding(m_IndexCountsBinding = new RawMapBinding<string>("l10n", "indexCounts", BindIndexCounts));
		AddBinding(new TriggerBinding<string>("l10n", "selectLocale", SelectLocale));
		m_LocalizationManager.onSupportedLocalesChanged += OnSupportedLocalesChanged;
		m_LocalizationManager.onActiveDictionaryChanged += OnActiveDictionaryChanged;
	}

	public void Dispose()
	{
		m_LocalizationManager.onSupportedLocalesChanged -= OnSupportedLocalesChanged;
		m_LocalizationManager.onActiveDictionaryChanged -= OnActiveDictionaryChanged;
	}

	private void OnSupportedLocalesChanged()
	{
		m_LocalesBinding.Update();
	}

	private void OnActiveDictionaryChanged()
	{
		m_ActiveDictionaryChangedBinding.Trigger();
		m_IndexCountsBinding.UpdateAll();
	}

	private void BindIndexCounts(IJsonWriter binder, string key)
	{
		binder.Write(m_LocalizationManager.activeDictionary.indexCounts.TryGetValue(key, out var value) ? value : 0);
	}

	private void SelectLocale(string localeID)
	{
		m_LocalizationManager?.SetActiveLocale(localeID);
		InterfaceSettings interfaceSettings = SharedSettings.instance?.userInterface;
		if (interfaceSettings != null)
		{
			interfaceSettings.locale = localeID;
		}
	}
}
