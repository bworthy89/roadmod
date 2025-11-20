using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Colossal.Json;
using Colossal.Reflection;
using Game.Rendering;
using Game.UI.Menu;
using Game.UI.Widgets;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game.Settings;

public abstract class QualitySetting : Setting
{
	public enum Level
	{
		Disabled,
		VeryLow,
		Low,
		Medium,
		High,
		Colossal,
		Custom
	}

	[Exclude]
	[IgnoreEquals]
	[SettingsUIHidden]
	public bool disableSetting { get; set; }

	public abstract Level GetLevel();

	public abstract void SetLevel(Level quality, bool apply = true);

	public abstract void TransferValuesFrom(QualitySetting setting);

	public abstract IEnumerable<Level> EnumerateAvailableLevels();

	public abstract string GetMockName(Level level);

	public override void SetDefaults()
	{
	}

	public DropdownItem<int>[] GetQualityValues()
	{
		Level[] array = EnumerateAvailableLevels().ToArray();
		List<DropdownItem<int>> list = new List<DropdownItem<int>>(array.Length);
		Level[] array2 = array;
		foreach (Level level in array2)
		{
			DropdownItem<int> dropdownItem = new DropdownItem<int>();
			dropdownItem.displayName = "Options." + level.GetType().Name.ToUpperInvariant() + "[" + GetMockName(level) + "]";
			dropdownItem.value = (int)level;
			dropdownItem.disabled = level == Level.Custom;
			DropdownItem<int> item = dropdownItem;
			list.Add(item);
		}
		return list.ToArray();
	}

	internal virtual void AddToPageData(AutomaticSettings.SettingPageData pageData)
	{
		AutomaticSettings.FillSettingsPage(pageData, this);
	}

	public virtual bool IsOptionsDisabled()
	{
		if (!IsOptionFullyDisabled())
		{
			return GetLevel() == Level.Disabled;
		}
		return true;
	}

	public virtual bool IsOptionFullyDisabled()
	{
		return disableSetting;
	}
}
public abstract class QualitySetting<T> : QualitySetting where T : QualitySetting
{
	private static readonly Dictionary<Level, string> s_MockNames;

	protected static readonly Dictionary<Level, T> s_SettingsMap;

	static QualitySetting()
	{
		s_MockNames = new Dictionary<Level, string>();
		s_SettingsMap = new Dictionary<Level, T>();
		s_SettingsMap = new Dictionary<Level, T>();
	}

	internal override void AddToPageData(AutomaticSettings.SettingPageData pageData)
	{
		AutomaticSettings.ManualProperty property = new AutomaticSettings.ManualProperty(GetType(), typeof(Level), "Level")
		{
			canRead = true,
			canWrite = true,
			getter = (object settings) => ((QualitySetting<T>)settings).GetLevel(),
			setter = delegate(object settings, object value)
			{
				((QualitySetting<T>)settings).SetLevel((Level)value);
			},
			attributes = 
			{
				(Attribute)new SettingsUIDropdownAttribute(typeof(QualitySetting), "GetQualityValues"),
				(Attribute)new SettingsUIPathAttribute(GetType().Name),
				(Attribute)new SettingsUIDisplayNameAttribute(GetType().Name),
				(Attribute)new SettingsUIDisableByConditionAttribute(typeof(QualitySetting), "IsOptionFullyDisabled")
			}
		};
		AutomaticSettings.SettingItemData item = new AutomaticSettings.SettingItemData(AutomaticSettings.WidgetType.AdvancedEnumDropdown, this, property, pageData.prefix)
		{
			isAdvanced = false,
			simpleGroup = "Quality",
			advancedGroup = GetType().Name
		};
		pageData["General"].AddItem(item);
		base.AddToPageData(pageData);
	}

	public override string GetMockName(Level level)
	{
		return MockName(level);
	}

	public static void RegisterMockName(Level level, string name)
	{
		s_MockNames[level] = name;
	}

	public static string MockName(Level level)
	{
		if (s_MockNames.TryGetValue(level, out var value))
		{
			return value;
		}
		return level.ToString();
	}

	public static void RegisterSetting(Level quality, T setting)
	{
		if (quality == Level.Custom)
		{
			UnityEngine.Debug.LogWarning("Can not register a default Custom quality setting. Ignoring.");
		}
		else
		{
			s_SettingsMap[quality] = setting;
		}
	}

	public override Level GetLevel()
	{
		foreach (KeyValuePair<Level, T> item in s_SettingsMap)
		{
			if (Equals(item.Value))
			{
				return item.Key;
			}
		}
		return Level.Custom;
	}

	public override IEnumerable<Level> EnumerateAvailableLevels()
	{
		foreach (Level availableLevel in GetAvailableLevels())
		{
			yield return availableLevel;
		}
	}

	public static IReadOnlyList<Level> GetAvailableLevels()
	{
		List<Level> list = new List<Level>(s_SettingsMap.Keys);
		list.Add(Level.Custom);
		list.Sort();
		return list;
	}

	protected void ApplyState<PT>(VolumeParameter<PT> param, PT value, bool state = true)
	{
		param.overrideState = state;
		param.value = value;
	}

	protected void CreateVolumeComponent<PT>(VolumeProfile profile, ref PT component) where PT : VolumeComponent
	{
		VolumeHelper.GetOrCreateVolumeComponent(profile, ref component);
	}

	public override void SetLevel(Level quality, bool apply = true)
	{
		if (s_SettingsMap.TryGetValue(quality, out var value))
		{
			TransferValuesFrom(value);
			if (apply)
			{
				ApplyAndSave();
			}
		}
		else
		{
			Setting.log.WarnFormat("Quality setting {0} doesn't exist for {1}", quality, GetType().Name);
		}
	}

	public override void TransferValuesFrom(QualitySetting setting)
	{
		Type type = GetType();
		bool flag = true;
		PropertyInfo property = type.GetProperty("enabled", BindingFlags.Instance | BindingFlags.Public);
		if (property != null)
		{
			flag = (bool)property.GetValue(setting);
			property.SetValue(this, flag);
		}
		if (!flag && setting.GetLevel() == Level.Disabled)
		{
			return;
		}
		PropertyInfo[] properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public);
		foreach (PropertyInfo propertyInfo in properties)
		{
			if (ReflectionUtils.GetAttribute<IgnoreEqualsAttribute>(propertyInfo.GetCustomAttributes(inherit: false)) == null)
			{
				propertyInfo.SetValue(this, propertyInfo.GetValue(setting));
			}
		}
	}
}
