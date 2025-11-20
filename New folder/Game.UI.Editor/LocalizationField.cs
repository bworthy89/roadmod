using System.Collections.Generic;
using System.Linq;
using Colossal.IO.AssetDatabase;
using Colossal.UI.Binding;
using Game.SceneFlow;
using Game.UI.Localization;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public class LocalizationField : Widget
{
	public struct LocalizationFieldEntry : IJsonWritable
	{
		public string localeId { get; set; }

		public string text { get; set; }

		public LocalizationFieldEntry(string localeId, string text)
		{
			this.localeId = localeId;
			this.text = text;
		}

		public void Write(IJsonWriter writer)
		{
			writer.TypeBegin("LocalizationFieldEntry");
			writer.PropertyName("localeId");
			writer.Write(localeId);
			writer.PropertyName("text");
			writer.Write(text);
			writer.TypeEnd();
		}
	}

	public class Bindings : IWidgetBindingFactory
	{
		public IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged)
		{
			yield return new TriggerBinding<IWidget, int, string, string>(group, "setLocalizationEntry", delegate(IWidget widget, int index, string localeId, string text)
			{
				if (widget is LocalizationField localizationField)
				{
					localizationField.SetEntry(index, localeId, text);
					onValueChanged(widget);
				}
			}, pathResolver);
			yield return new TriggerBinding<IWidget>(group, "addLocalizationEntry", delegate(IWidget widget)
			{
				if (widget is LocalizationField localizationField)
				{
					localizationField.AddLanguage();
					onValueChanged(widget);
				}
			}, pathResolver);
			yield return new TriggerBinding<IWidget, int>(group, "removeLocalizationEntry", delegate(IWidget widget, int index)
			{
				if (widget is LocalizationField localizationField)
				{
					localizationField.RemoveLanguage(index);
					onValueChanged(widget);
				}
			}, pathResolver);
		}
	}

	private List<LocalizationFieldEntry> m_Localization = new List<LocalizationFieldEntry>();

	public IReadOnlyList<LocalizationFieldEntry> localization => m_Localization;

	public LocalizedString placeholder { get; set; }

	public LocalizationField(LocalizedString placeholder)
	{
		this.placeholder = placeholder;
		Initialize();
	}

	public void Clear()
	{
		m_Localization.Clear();
	}

	public void Initialize()
	{
		Clear();
		InitializeMandatory();
		if (m_Localization.FindIndex((LocalizationFieldEntry loc) => loc.localeId == GameManager.instance.localizationManager.activeLocaleId) < 0)
		{
			m_Localization.Add(new LocalizationFieldEntry
			{
				localeId = GameManager.instance.localizationManager.activeLocaleId,
				text = string.Empty
			});
		}
	}

	public void Initialize(IEnumerable<LocaleAsset> assets, string localeFormat)
	{
		Clear();
		InitializeMandatory();
		if (assets == null)
		{
			return;
		}
		foreach (LocaleAsset asset in assets)
		{
			Add(asset, localeFormat);
		}
	}

	public void Initialize(IEnumerable<LocalizationFieldEntry> entries)
	{
		Clear();
		InitializeMandatory();
		foreach (LocalizationFieldEntry entry in entries)
		{
			Add(entry);
		}
	}

	public static bool IsMandatory(string localeId)
	{
		return localeId == GameManager.instance.localizationManager.fallbackLocaleId;
	}

	private void InitializeMandatory()
	{
		if (!string.IsNullOrEmpty(GameManager.instance.localizationManager.fallbackLocaleId))
		{
			int num = m_Localization.FindIndex((LocalizationFieldEntry l) => IsMandatory(l.localeId));
			if (num < 0)
			{
				m_Localization.Add(new LocalizationFieldEntry
				{
					localeId = GameManager.instance.localizationManager.fallbackLocaleId,
					text = string.Empty
				});
			}
			else if (num > 0)
			{
				List<LocalizationFieldEntry> list = m_Localization;
				List<LocalizationFieldEntry> list2 = m_Localization;
				int index = num;
				LocalizationFieldEntry localizationFieldEntry = m_Localization[num];
				LocalizationFieldEntry localizationFieldEntry2 = m_Localization[0];
				LocalizationFieldEntry localizationFieldEntry3 = (list[0] = localizationFieldEntry);
				localizationFieldEntry3 = (list2[index] = localizationFieldEntry2);
			}
		}
	}

	public void Add(LocaleAsset asset, string localeFormat)
	{
		if (asset == null)
		{
			return;
		}
		foreach (string key in asset.data.entries.Keys)
		{
			if (key == localeFormat && !string.IsNullOrEmpty(asset.data.entries[key]))
			{
				int num = m_Localization.FindIndex((LocalizationFieldEntry loc) => loc.localeId == asset.localeId);
				if (num >= 0)
				{
					LocalizationFieldEntry value = m_Localization[num];
					value.text = asset.data.entries[key];
					m_Localization[num] = value;
				}
				else
				{
					m_Localization.Add(new LocalizationFieldEntry
					{
						localeId = asset.localeId,
						text = asset.data.entries[key]
					});
				}
			}
		}
	}

	public void Add(LocalizationFieldEntry entry)
	{
		int num = m_Localization.FindIndex((LocalizationFieldEntry loc) => loc.localeId == entry.localeId);
		if (num < 0)
		{
			m_Localization.Add(entry);
		}
		else
		{
			m_Localization[num] = entry;
		}
	}

	public void SetEntry(int index, string localeId, string text)
	{
		LocalizationFieldEntry value = m_Localization[index];
		value.localeId = localeId;
		value.text = text;
		m_Localization[index] = value;
		SetPropertiesChanged();
	}

	public void AddLanguage()
	{
		if (TryGetUnusedLanguage(out var localeID))
		{
			m_Localization.Add(new LocalizationFieldEntry
			{
				localeId = localeID,
				text = string.Empty
			});
		}
		SetPropertiesChanged();
	}

	public void RemoveLanguage(int index)
	{
		if (IsMandatory(m_Localization[index].localeId))
		{
			LocalizationFieldEntry value = m_Localization[index];
			value.text = string.Empty;
			m_Localization[index] = value;
		}
		else
		{
			m_Localization.RemoveAt(index);
		}
		SetPropertiesChanged();
	}

	public bool IsValid()
	{
		if (!string.IsNullOrEmpty(GameManager.instance.localizationManager.fallbackLocaleId))
		{
			int num = m_Localization.FindIndex((LocalizationFieldEntry loc) => loc.localeId == GameManager.instance.localizationManager.fallbackLocaleId);
			if (num >= 0)
			{
				return IsValid(m_Localization[num]);
			}
		}
		return ValidEntries().Any();
	}

	public IEnumerable<LocalizationFieldEntry> ValidEntries()
	{
		foreach (LocalizationFieldEntry item in m_Localization)
		{
			if (IsValid(item))
			{
				yield return item;
			}
		}
	}

	private bool IsValid(LocalizationFieldEntry entry)
	{
		return !string.IsNullOrWhiteSpace(entry.text);
	}

	private bool TryGetUnusedLanguage(out string localeID)
	{
		if (m_Localization.FindIndex((LocalizationFieldEntry entry) => entry.localeId == GameManager.instance.localizationManager.activeLocaleId) < 0)
		{
			localeID = GameManager.instance.localizationManager.activeLocaleId;
			return true;
		}
		string[] supportedLocales = GameManager.instance.localizationManager.GetSupportedLocales();
		foreach (string locale in supportedLocales)
		{
			if (m_Localization.FindIndex((LocalizationFieldEntry entry) => entry.localeId == locale) < 0)
			{
				localeID = locale;
				return true;
			}
		}
		localeID = null;
		return false;
	}

	public void BuildLocaleData(string format, Dictionary<string, LocaleData> localeDatas, string fallback = null)
	{
		foreach (LocalizationFieldEntry item in m_Localization)
		{
			if (IsValid(item))
			{
				if (!localeDatas.ContainsKey(item.localeId))
				{
					localeDatas[item.localeId] = new LocaleData(item.localeId, new Dictionary<string, string>(), new Dictionary<string, int>());
				}
				localeDatas[item.localeId].entries[format] = item.text;
			}
		}
		if (fallback != null)
		{
			string fallbackLocaleId = GameManager.instance.localizationManager.fallbackLocaleId;
			if (!localeDatas.ContainsKey(fallbackLocaleId))
			{
				localeDatas[fallbackLocaleId] = new LocaleData(fallbackLocaleId, new Dictionary<string, string>(), new Dictionary<string, int>());
			}
			localeDatas[fallbackLocaleId].entries.TryAdd(format, fallback);
		}
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("supportedLocales");
		writer.Write(GameManager.instance.localizationManager.GetSupportedLocales());
		writer.PropertyName("localization");
		writer.Write((IList<LocalizationFieldEntry>)m_Localization);
		writer.PropertyName("placeholder");
		writer.Write(placeholder);
		writer.PropertyName("mandatoryId");
		writer.Write(GameManager.instance.localizationManager.fallbackLocaleId);
	}
}
