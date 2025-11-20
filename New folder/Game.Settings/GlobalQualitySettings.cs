using System;
using System.Collections.Generic;
using Colossal.Json;

namespace Game.Settings;

public class GlobalQualitySettings : QualitySetting<GlobalQualitySettings>
{
	protected static readonly Dictionary<Type, Level> s_DefaultsMap = new Dictionary<Type, Level>();

	private List<QualitySetting> m_QualitySettings = new List<QualitySetting>();

	[MergeByType]
	[IgnoreEquals]
	public List<QualitySetting> qualitySettings
	{
		get
		{
			return m_QualitySettings;
		}
		set
		{
			foreach (QualitySetting qualitySetting in m_QualitySettings)
			{
				QualitySetting qualitySetting2 = value.Find((QualitySetting x) => qualitySetting.GetType() == x.GetType());
				if (qualitySetting2 != null)
				{
					qualitySetting.TransferValuesFrom(qualitySetting2);
				}
			}
		}
	}

	[Exclude]
	[IgnoreEquals]
	[SettingsUIHidden]
	public int countQualitySettings => m_QualitySettings.Count;

	public QualitySetting lastSetting
	{
		get
		{
			List<QualitySetting> list = m_QualitySettings;
			return list[list.Count - 1];
		}
	}

	public override bool Equals(object obj)
	{
		if (!(obj is GlobalQualitySettings globalQualitySettings))
		{
			return false;
		}
		bool result = base.Equals(obj);
		for (int i = 0; i < m_QualitySettings.Count; i++)
		{
			if (!m_QualitySettings[i].Equals(globalQualitySettings.m_QualitySettings[i]))
			{
				return false;
			}
		}
		return result;
	}

	public override int GetHashCode()
	{
		int num = base.GetHashCode();
		foreach (QualitySetting item in EnumerateQualitySettings())
		{
			num = (num * 937) ^ item.GetHashCode();
		}
		return num;
	}

	public T GetQualitySetting<T>() where T : QualitySetting
	{
		foreach (QualitySetting qualitySetting in m_QualitySettings)
		{
			if (qualitySetting.GetType() == typeof(T))
			{
				return (T)qualitySetting;
			}
		}
		return null;
	}

	public QualitySetting GetQualitySetting(Type type)
	{
		foreach (QualitySetting qualitySetting in m_QualitySettings)
		{
			if (qualitySetting.GetType() == type)
			{
				return qualitySetting;
			}
		}
		return null;
	}

	public override void SetDefaults()
	{
		foreach (QualitySetting item in EnumerateQualitySettings())
		{
			item.SetLevel(s_DefaultsMap[item.GetType()], apply: false);
		}
	}

	public void AddQualitySetting<T>(T setting) where T : QualitySetting<T>
	{
		Type typeFromHandle = typeof(T);
		if (GetQualitySetting<T>() != null)
		{
			return;
		}
		s_DefaultsMap[typeFromHandle] = setting.GetLevel();
		m_QualitySettings.Add(setting);
		foreach (KeyValuePair<Level, GlobalQualitySettings> item in QualitySetting<GlobalQualitySettings>.s_SettingsMap)
		{
			if (item.Value.GetQualitySetting<T>() != null)
			{
				continue;
			}
			Level key = item.Key;
			if (!QualitySetting<T>.s_SettingsMap.TryGetValue(key, out var value))
			{
				Level level = key - 1;
				while (level >= Level.Disabled && !QualitySetting<T>.s_SettingsMap.TryGetValue(level, out value))
				{
					level--;
				}
				if (value == null)
				{
					for (Level level2 = key + 1; level2 < Level.Custom && !QualitySetting<T>.s_SettingsMap.TryGetValue(level2, out value); level2++)
					{
					}
				}
			}
			item.Value.m_QualitySettings.Add(value);
		}
	}

	public IEnumerable<QualitySetting> EnumerateQualitySettings()
	{
		foreach (QualitySetting qualitySetting in m_QualitySettings)
		{
			yield return qualitySetting;
		}
	}

	public override void SetLevel(Level quality, bool apply = true)
	{
		if (quality < Level.Custom)
		{
			for (int i = 0; i < m_QualitySettings.Count; i++)
			{
				m_QualitySettings[i].TransferValuesFrom(QualitySetting<GlobalQualitySettings>.s_SettingsMap[quality].m_QualitySettings[i]);
			}
			if (apply)
			{
				ApplyAndSave();
			}
		}
	}
}
