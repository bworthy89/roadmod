using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using Colossal.Json;
using Colossal.OdinSerializer.Utilities;
using Colossal.Reflection;
using Colossal.UI.Binding;
using Game.Input;
using Game.Reflection;
using Game.SceneFlow;
using Game.Settings;
using Game.UI.Localization;
using Game.UI.Widgets;
using Unity.Entities;
using UnityEngine;

namespace Game.UI.Menu;

public static class AutomaticSettings
{
	private struct SectionInfo
	{
		public string m_Tab;

		public string m_SimpleGroup;

		public string m_AdvancedGroup;
	}

	public enum WidgetType
	{
		None,
		BoolButton,
		BoolButtonWithConfirmation,
		BoolToggle,
		IntDropdown,
		IntSlider,
		FloatSlider,
		StringDropdown,
		StringField,
		StringTextInput,
		LocalizedStringField,
		AdvancedEnumDropdown,
		EnumDropdown,
		KeyBinding,
		DirectoryPicker,
		MultilineText,
		CustomDropdown
	}

	public class SettingPageData
	{
		private Dictionary<string, int> m_TabOrder = new Dictionary<string, int>();

		private Dictionary<string, int> m_GroupOrder = new Dictionary<string, int>();

		private List<SettingTabData> m_Tabs = new List<SettingTabData>();

		public string id { get; }

		public bool addPrefix { get; }

		public string prefix
		{
			get
			{
				if (!addPrefix)
				{
					return string.Empty;
				}
				return id;
			}
		}

		public bool showAllGroupNames { get; set; }

		private HashSet<string> m_GroupToShowName { get; } = new HashSet<string>();

		public List<SettingTabData> tabs => m_Tabs;

		public IEnumerable<string> groupNames => from g in m_GroupOrder
			orderby g.Value
			select g.Key;

		public IEnumerable<string> groupToShowName
		{
			get
			{
				if (!showAllGroupNames)
				{
					return m_GroupToShowName;
				}
				return groupNames;
			}
		}

		public Func<bool> warningGetter { get; set; }

		public Dictionary<string, Func<bool>> tabWarningGetters { get; set; }

		public SettingTabData this[string name]
		{
			get
			{
				int num = m_Tabs.FindIndex((SettingTabData s) => s.id == name);
				if (num != -1)
				{
					return m_Tabs[num];
				}
				SettingTabData settingTabData = new SettingTabData(name, this);
				m_Tabs.Add(settingTabData);
				return settingTabData;
			}
		}

		public SettingPageData(string id, bool addPrefix)
		{
			this.id = id;
			this.addPrefix = addPrefix;
		}

		public void SortTabs()
		{
			m_Tabs.Sort(delegate(SettingTabData a, SettingTabData b)
			{
				if (!m_TabOrder.TryGetValue(a.id, out var value))
				{
					value = int.MaxValue;
				}
				if (!m_TabOrder.TryGetValue(b.id, out var value2))
				{
					value2 = int.MaxValue;
				}
				return value.CompareTo(value2);
			});
		}

		public OptionsUISystem.Page BuildPage()
		{
			OptionsUISystem.Page page = new OptionsUISystem.Page
			{
				id = id,
				beta = BetaFilter.options.Contains(id),
				warningGetter = warningGetter
			};
			SortTabs();
			foreach (SettingTabData tab in m_Tabs)
			{
				page.sections.Add(tab.BuildTab(page));
			}
			return page;
		}

		public void AddTab(string tab)
		{
			m_TabOrder.TryAdd(tab, m_TabOrder.Count);
		}

		public void AddGroup(string group)
		{
			m_GroupOrder.TryAdd(group, m_GroupOrder.Count);
		}

		public bool TryGetTabOrder(string tabName, out int index)
		{
			return m_TabOrder.TryGetValue(tabName, out index);
		}

		public bool TryGetGroupOrder(string groupName, out int index)
		{
			return m_GroupOrder.TryGetValue(groupName, out index);
		}

		public void AddGroupToShowName(string group)
		{
			m_GroupToShowName.Add(group);
		}
	}

	public class SettingTabData
	{
		private readonly List<SettingItemData> m_Items = new List<SettingItemData>();

		public string id { get; }

		public SettingPageData pageData { get; }

		public IEnumerable<SettingItemData> items => m_Items;

		public SettingTabData(string id, SettingPageData pageData)
		{
			this.id = id;
			this.pageData = pageData;
		}

		public void AddItem(SettingItemData item)
		{
			m_Items.Add(item);
		}

		public void InsertItem(SettingItemData item, int index)
		{
			if (index > m_Items.Count)
			{
				AddItem(item);
			}
			else
			{
				m_Items.Insert(index, item);
			}
		}

		public OptionsUISystem.Section BuildTab(OptionsUISystem.Page page)
		{
			Func<bool> value;
			OptionsUISystem.Section section = new OptionsUISystem.Section(pageData.addPrefix ? (pageData.id + "." + id) : id, page, pageData)
			{
				warningGetter = (pageData.tabWarningGetters.TryGetValue(id, out value) ? value : null)
			};
			foreach (SettingItemData item in m_Items)
			{
				IWidget widget = item.widget;
				if (widget != null)
				{
					widget.Update();
					section.options.Add(new OptionsUISystem.Option
					{
						widget = widget,
						isAdvanced = item.isAdvanced,
						simpleGroupIndex = (pageData.TryGetGroupOrder(item.simpleGroup ?? string.Empty, out var index) ? index : int.MaxValue),
						advancedGroupIndex = (pageData.TryGetGroupOrder(item.advancedGroup ?? item.simpleGroup ?? string.Empty, out var index2) ? index2 : int.MaxValue),
						searchHidden = item.isSearchHidden
					});
				}
			}
			return section;
		}
	}

	public class SettingItemData
	{
		private IWidget m_Widget;

		public WidgetType widgetType { get; }

		public Setting setting { get; }

		public IProxyProperty property { get; }

		public string prefix { get; }

		public string path { get; }

		public LocalizedString displayName { get; set; }

		public LocalizedString description { get; set; }

		public bool isAdvanced { get; set; }

		public string simpleGroup { get; set; }

		public string advancedGroup { get; set; }

		public bool isSearchHidden { get; set; }

		public Delegate setterAction { get; set; }

		public Func<bool> disableAction { get; set; }

		public Func<bool> hideAction { get; set; }

		public Func<int> valueVersionAction { get; set; }

		public Func<LocalizedString> dispayNameAction { get; set; }

		public Func<LocalizedString> descriptionAction { get; set; }

		public Func<bool> warningAction { get; set; }

		public IWidget widget => m_Widget ?? (m_Widget = GetWidget());

		protected virtual IWidget GetWidget()
		{
			return widgetType switch
			{
				WidgetType.BoolButtonWithConfirmation => AddBoolButtonWithConfirmationProperty(this), 
				WidgetType.BoolToggle => AddBoolToggleProperty(this), 
				WidgetType.BoolButton => AddBoolButtonProperty(this), 
				WidgetType.IntDropdown => AddIntDropdownProperty(this), 
				WidgetType.IntSlider => AddIntSliderProperty(this), 
				WidgetType.FloatSlider => AddFloatSliderProperty(this), 
				WidgetType.StringDropdown => AddStringDropdownProperty(this), 
				WidgetType.StringField => AddStringFieldProperty(this), 
				WidgetType.StringTextInput => AddStringTextInputProperty(this), 
				WidgetType.LocalizedStringField => AddLocalizedStringFieldProperty(this), 
				WidgetType.AdvancedEnumDropdown => AddEnumDropdownProperty(this), 
				WidgetType.EnumDropdown => AddEnumSimpleProperty(this), 
				WidgetType.KeyBinding => AddKeyBindingProperty(this), 
				WidgetType.DirectoryPicker => AddDirectoryPickerBindingProperty(this), 
				WidgetType.CustomDropdown => AddCustomDropdownProperty(this), 
				_ => null, 
			};
		}

		public SettingItemData(WidgetType widgetType, Setting setting, IProxyProperty property, string prefix)
		{
			this.widgetType = widgetType;
			this.setting = setting;
			this.property = property;
			this.prefix = prefix;
			path = GetPath(property, prefix);
			displayName = GetDisplayName(property, path);
			description = GetDescription(property, path);
			dispayNameAction = GetDisplayNameAction(property, setting);
			descriptionAction = GetDescriptionAction(property, setting);
			setterAction = GetSetterAction(property, setting);
			disableAction = GetDisableAction(property, setting);
			hideAction = GetHideAction(property, setting);
			valueVersionAction = GetValueVersionAction(property, setting);
			isAdvanced = IsAdvanced(property);
			isSearchHidden = IsSearchHidden(property);
			warningAction = GetWarningAction(property, setting);
		}

		private string GetPath(IProxyProperty property, string prefix)
		{
			string text = property.GetAttribute<SettingsUIPathAttribute>()?.path;
			if (string.IsNullOrEmpty(text))
			{
				text = property.declaringType.Name + "." + property.name;
				if (!string.IsNullOrEmpty(prefix))
				{
					text = prefix + "." + text;
				}
			}
			return text;
		}

		private LocalizedString GetDisplayName(IProxyProperty property, string path)
		{
			SettingsUIDisplayNameAttribute attribute = property.GetAttribute<SettingsUIDisplayNameAttribute>();
			if (attribute != null)
			{
				if (!string.IsNullOrEmpty(attribute.id))
				{
					return LocalizedString.IdWithFallback("Options.OPTION[" + attribute.id + "]", attribute.value);
				}
				if (!string.IsNullOrEmpty(attribute.value))
				{
					return LocalizedString.Value(attribute.value);
				}
			}
			return LocalizedString.Id("Options.OPTION[" + path + "]");
		}

		private LocalizedString GetDescription(IProxyProperty property, string path)
		{
			SettingsUIDescriptionAttribute attribute = property.GetAttribute<SettingsUIDescriptionAttribute>();
			if (attribute != null)
			{
				if (!string.IsNullOrEmpty(attribute.id))
				{
					return LocalizedString.IdWithFallback("Options.OPTION_DESCRIPTION[" + attribute.id + "]", attribute.value);
				}
				if (!string.IsNullOrEmpty(attribute.value))
				{
					return LocalizedString.Value(attribute.value);
				}
			}
			return LocalizedString.Id("Options.OPTION_DESCRIPTION[" + path + "]");
		}

		private Func<LocalizedString> GetDisplayNameAction(IProxyProperty property, Setting setting)
		{
			SettingsUIDisplayNameAttribute attribute = property.GetAttribute<SettingsUIDisplayNameAttribute>();
			if (attribute != null && TryGetAction(setting, attribute.getterType, attribute.getterMethod, out Func<LocalizedString> action))
			{
				return action;
			}
			return null;
		}

		private Func<LocalizedString> GetDescriptionAction(IProxyProperty property, Setting setting)
		{
			SettingsUIDescriptionAttribute attribute = property.GetAttribute<SettingsUIDescriptionAttribute>();
			if (attribute != null && TryGetAction(setting, attribute.getterType, attribute.getterMethod, out Func<LocalizedString> action))
			{
				return action;
			}
			return null;
		}

		private Func<bool> GetWarningAction(IProxyProperty property, Setting setting)
		{
			SettingsUIWarningAttribute attribute = property.GetAttribute<SettingsUIWarningAttribute>();
			if (attribute != null && TryGetAction(setting, attribute.checkType, attribute.checkMethod, out Func<bool> action))
			{
				return action;
			}
			return null;
		}

		private bool IsAdvanced(IProxyProperty property)
		{
			if (property.GetAttribute<SettingsUIAdvancedAttribute>() != null)
			{
				return true;
			}
			return ReflectionUtils.GetAttribute<SettingsUIAdvancedAttribute>(property.declaringType.GetCustomAttributes(inherit: false)) != null;
		}

		private bool IsSearchHidden(IProxyProperty property)
		{
			if (property.GetAttribute<SettingsUISearchHiddenAttribute>() != null)
			{
				return true;
			}
			return ReflectionUtils.GetAttribute<SettingsUISearchHiddenAttribute>(property.declaringType.GetCustomAttributes(inherit: false)) != null;
		}

		private Delegate GetSetterAction(IProxyProperty property, Setting setting)
		{
			SettingsUISetterAttribute attribute = property.GetAttribute<SettingsUISetterAttribute>();
			if (attribute == null || attribute.setterType == null || string.IsNullOrEmpty(attribute.setterMethod))
			{
				return null;
			}
			BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
			if (attribute.setterType.IsInstanceOfType(setting))
			{
				bindingFlags |= BindingFlags.Instance;
			}
			MethodInfo[] methods = attribute.setterType.GetMethods(bindingFlags);
			foreach (MethodInfo methodInfo in methods)
			{
				if (methodInfo.Name != attribute.setterMethod || methodInfo.ReturnType != typeof(void))
				{
					continue;
				}
				ParameterInfo[] parameters = methodInfo.GetParameters();
				if (parameters.Length != 1)
				{
					continue;
				}
				if (!property.propertyType.IsEnum)
				{
					if (parameters[0].ParameterType != property.propertyType)
					{
						return null;
					}
				}
				else if (parameters[0].ParameterType != property.propertyType && parameters[0].ParameterType != typeof(int) && parameters[0].ParameterType != property.propertyType.GetEnumUnderlyingType())
				{
					return null;
				}
				Type delegateType = typeof(Action<>).MakeGenericType(parameters[0].ParameterType);
				return methodInfo.CreateDelegate(delegateType, methodInfo.IsStatic ? null : setting);
			}
			return null;
		}

		private Func<bool> GetDisableAction(IProxyProperty property, Setting setting)
		{
			SettingsUIDisableByConditionAttribute attribute = property.GetAttribute<SettingsUIDisableByConditionAttribute>();
			if (attribute != null && TryGetAction(setting, attribute.checkType, attribute.checkMethod, out Func<bool> action))
			{
				if (!attribute.invert)
				{
					return action;
				}
				return () => !action();
			}
			attribute = ReflectionUtils.GetAttribute<SettingsUIDisableByConditionAttribute>(property.declaringType.GetCustomAttributes(inherit: false));
			if (attribute != null && TryGetAction(setting, attribute.checkType, attribute.checkMethod, out action))
			{
				if (!attribute.invert)
				{
					return action;
				}
				return () => !action();
			}
			return null;
		}

		private Func<bool> GetHideAction(IProxyProperty property, Setting setting)
		{
			SettingsUIHideByConditionAttribute attribute = property.GetAttribute<SettingsUIHideByConditionAttribute>();
			if (attribute != null && TryGetAction(setting, attribute.checkType, attribute.checkMethod, out Func<bool> action))
			{
				if (!attribute.invert)
				{
					return action;
				}
				return () => !action();
			}
			attribute = ReflectionUtils.GetAttribute<SettingsUIHideByConditionAttribute>(property.declaringType.GetCustomAttributes(inherit: false));
			if (attribute != null && TryGetAction(setting, attribute.checkType, attribute.checkMethod, out action))
			{
				if (!attribute.invert)
				{
					return action;
				}
				return () => !action();
			}
			return null;
		}

		private Func<int> GetValueVersionAction(IProxyProperty property, Setting setting)
		{
			SettingsUIValueVersionAttribute attribute = property.GetAttribute<SettingsUIValueVersionAttribute>();
			if (attribute != null && TryGetAction(setting, attribute.versionGetterType, attribute.versionGetterMethod, out Func<int> action))
			{
				return action;
			}
			return null;
		}
	}

	public interface IProxyProperty
	{
		bool canRead { get; }

		bool canWrite { get; }

		string name { get; }

		Type propertyType { get; }

		Type declaringType { get; }

		void SetValue(object obj, object value);

		object GetValue(object obj);

		bool HasAttribute<T>(bool inherit = false) where T : Attribute;

		T GetAttribute<T>(bool inherit = false) where T : Attribute;

		IEnumerable<T> GetAttributes<T>(bool inherit = false) where T : Attribute;

		bool TryGetAttribute<T>(out T attribute, bool inherit = false) where T : Attribute;
	}

	public class ProxyProperty : IProxyProperty
	{
		public PropertyInfo property { get; }

		public bool canRead => property.CanRead;

		public bool canWrite => property.CanWrite;

		public string name => property.Name;

		public Type propertyType => property.PropertyType;

		public Type declaringType => property.DeclaringType;

		public ProxyProperty(PropertyInfo property)
		{
			this.property = property;
		}

		public void SetValue(object obj, object value)
		{
			property.SetValue(obj, value);
		}

		public object GetValue(object obj)
		{
			return property.GetValue(obj);
		}

		public bool HasAttribute<T>(bool inherit = false) where T : Attribute
		{
			return property.HasAttribute<T>(inherit);
		}

		public T GetAttribute<T>(bool inherit = false) where T : Attribute
		{
			return property.GetAttribute<T>(inherit);
		}

		public IEnumerable<T> GetAttributes<T>(bool inherit = false) where T : Attribute
		{
			return property.GetAttributes<T>(inherit);
		}

		public bool TryGetAttribute<T>(out T attribute, bool inherit = false) where T : Attribute
		{
			return ((MemberInfo)property).TryGetAttribute(out attribute, inherit);
		}
	}

	public class ManualProperty : IProxyProperty
	{
		public bool canRead { get; set; }

		public bool canWrite { get; set; }

		public string name { get; set; }

		public Type propertyType { get; set; }

		public Type declaringType { get; set; }

		public Action<object, object> setter { get; set; }

		public Func<object, object> getter { get; set; }

		public List<Attribute> attributes { get; } = new List<Attribute>();

		public ManualProperty(Type declaringType, Type propertyType, string name)
		{
			this.declaringType = declaringType;
			this.propertyType = propertyType;
			this.name = name;
		}

		public void SetValue(object obj, object value)
		{
			setter?.Invoke(obj, value);
		}

		public object GetValue(object obj)
		{
			return getter?.Invoke(obj);
		}

		public bool HasAttribute<T>(bool inherit = false) where T : Attribute
		{
			return attributes.AnyOfType(typeof(T));
		}

		public T GetAttribute<T>(bool inherit = false) where T : Attribute
		{
			return attributes.OfType<T>().FirstOrDefault();
		}

		public IEnumerable<T> GetAttributes<T>(bool inherit = false) where T : Attribute
		{
			return attributes.OfType<T>();
		}

		public bool TryGetAttribute<T>(out T attribute, bool inherit = false) where T : Attribute
		{
			attribute = GetAttribute<T>();
			return attribute != null;
		}
	}

	public class DropdownItemsAccessor<T> : ITypedValueAccessor<T>, IValueAccessor
	{
		public Type valueType => typeof(T);

		public IValueAccessor parent => null;

		public Func<T> del { get; }

		public DropdownItemsAccessor(Type valueType, MethodInfo getterMethod, Setting setting)
		{
			Type type = typeof(DropdownItem<>).MakeGenericType(valueType).MakeArrayType();
			if (getterMethod.ReturnType != type)
			{
				throw new ArgumentException($"method's return type must be {type}", "getterMethod");
			}
			if (getterMethod.GetParameters().Length != 0)
			{
				throw new ArgumentException("method must take 0 arg", "getterMethod");
			}
			del = (Func<T>)getterMethod.CreateDelegate(typeof(Func<T>), setting);
		}

		public DropdownItemsAccessor(Func<T> del)
		{
			this.del = del;
		}

		public object GetValue()
		{
			if (del != null)
			{
				return del();
			}
			return null;
		}

		public T GetTypedValue()
		{
			return (T)GetValue();
		}

		public void SetValue(object value)
		{
			throw new InvalidOperationException("DropdownItemsAccessor is readonly");
		}

		public void SetTypedValue(T value)
		{
			throw new InvalidOperationException("DropdownItemsAccessor is readonly");
		}
	}

	private static readonly MethodInfo s_CustomDropdownMethodInfo;

	private static readonly MethodInfo s_EnumSetterMethodInfo;

	private static readonly Dictionary<string, ButtonRow> s_ButtonGroups;

	static AutomaticSettings()
	{
		s_CustomDropdownMethodInfo = typeof(AutomaticSettings).GetMethod("AddCustomDropdownPropertyGeneric", BindingFlags.Static | BindingFlags.Public);
		s_EnumSetterMethodInfo = typeof(AutomaticSettings).GetMethod("GetSetterActionGeneric", BindingFlags.Static | BindingFlags.NonPublic);
		s_ButtonGroups = new Dictionary<string, ButtonRow>();
	}

	private static bool GetButtonsGroup(string groupName, out ButtonRow buttons, Button item)
	{
		if (s_ButtonGroups.TryGetValue(groupName, out buttons))
		{
			List<Button> list = new List<Button>(buttons.children);
			list.Add(item);
			buttons.children = list.ToArray();
			return false;
		}
		buttons = new ButtonRow
		{
			children = new Button[1] { item }
		};
		s_ButtonGroups.Add(groupName, buttons);
		return true;
	}

	private static bool IsHidden(IProxyProperty property)
	{
		if (property.GetAttribute<SettingsUIHiddenAttribute>() != null)
		{
			return true;
		}
		return false;
	}

	private static bool IsSupportedOnPlatform(IProxyProperty property)
	{
		return property.GetAttribute<SettingsUIPlatformAttribute>()?.IsPlatformSet(Application.platform) ?? true;
	}

	private static bool IsDeveloperOnly(IProxyProperty property)
	{
		if (property.GetAttribute<SettingsUIDeveloperAttribute>() != null && !GameManager.instance.configuration.developerMode)
		{
			return true;
		}
		return false;
	}

	private static Dictionary<string, SectionInfo> GetSections(IProxyProperty property)
	{
		Dictionary<string, SectionInfo> dictionary = new Dictionary<string, SectionInfo>();
		foreach (SettingsUISectionAttribute attribute in property.GetAttributes<SettingsUISectionAttribute>())
		{
			dictionary[attribute.tab] = new SectionInfo
			{
				m_Tab = attribute.tab,
				m_SimpleGroup = attribute.simpleGroup,
				m_AdvancedGroup = attribute.advancedGroup
			};
		}
		if (dictionary.Count != 0)
		{
			return dictionary;
		}
		foreach (SettingsUISectionAttribute attribute2 in ReflectionUtils.GetAttributes<SettingsUISectionAttribute>(property.declaringType.GetCustomAttributes(inherit: false)))
		{
			dictionary[attribute2.tab] = new SectionInfo
			{
				m_Tab = attribute2.tab,
				m_SimpleGroup = attribute2.simpleGroup,
				m_AdvancedGroup = attribute2.advancedGroup
			};
		}
		if (dictionary.Count != 0)
		{
			return dictionary;
		}
		dictionary["General"] = new SectionInfo
		{
			m_Tab = "General",
			m_SimpleGroup = string.Empty,
			m_AdvancedGroup = string.Empty
		};
		return dictionary;
	}

	public static bool TryGetAction<T>(Setting setting, Type type, string name, out Func<T> action)
	{
		try
		{
			if (type != null && !string.IsNullOrEmpty(name))
			{
				BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
				if (type.IsInstanceOfType(setting))
				{
					bindingFlags |= BindingFlags.Instance;
				}
				MethodInfo[] methods = type.GetMethods(bindingFlags);
				foreach (MethodInfo methodInfo in methods)
				{
					if (!(methodInfo.Name != name) && !(methodInfo.ReturnType != typeof(T)) && methodInfo.GetParameters().Length == 0)
					{
						action = (Func<T>)methodInfo.CreateDelegate(typeof(Func<T>), methodInfo.IsStatic ? null : setting);
						return true;
					}
				}
				PropertyInfo[] properties = type.GetProperties(bindingFlags);
				foreach (PropertyInfo propertyInfo in properties)
				{
					if (!(propertyInfo.Name != name) && propertyInfo.CanRead && !(propertyInfo.PropertyType != typeof(T)))
					{
						MethodInfo getMethod = propertyInfo.GetGetMethod(nonPublic: true);
						action = (Func<T>)getMethod.CreateDelegate(typeof(Func<T>), getMethod.IsStatic ? null : setting);
						return true;
					}
				}
			}
		}
		catch (Exception innerException)
		{
			throw new Exception($"TryGetAction error with {name} in {type}", innerException);
		}
		action = null;
		return false;
	}

	private static DropdownItemsAccessor<DropdownItem<T>[]> GetDropdownItemAccessor<T>(IProxyProperty property, Setting setting)
	{
		SettingsUIDropdownAttribute attribute = property.GetAttribute<SettingsUIDropdownAttribute>();
		if (attribute != null && TryGetAction(setting, attribute.itemsGetterType, attribute.itemsGetterMethod, out Func<DropdownItem<T>[]> action))
		{
			return new DropdownItemsAccessor<DropdownItem<T>[]>(action);
		}
		return null;
	}

	private static DelegateAccessor<EnumMember[]> GetEnumMemberAccessor(IProxyProperty property, Setting setting, string prefix)
	{
		SettingsUIDropdownAttribute attribute = property.GetAttribute<SettingsUIDropdownAttribute>();
		if (attribute != null && TryGetAction(setting, attribute.itemsGetterType, attribute.itemsGetterMethod, out Func<EnumMember[]> action))
		{
			return new DelegateAccessor<EnumMember[]>(action);
		}
		prefix = (string.IsNullOrEmpty(prefix) ? "Options" : ("Options." + prefix));
		return new DelegateAccessor<EnumMember[]>(() => GetEnumValues(property.propertyType, prefix));
	}

	public static EnumMember[] GetEnumValues(Type enumType, string prefix)
	{
		if (!enumType.IsEnum)
		{
			throw new ArgumentException("Type is not an enum");
		}
		Type enumUnderlyingType = enumType.GetEnumUnderlyingType();
		List<EnumMember> list = new List<EnumMember>();
		string[] names = Enum.GetNames(enumType);
		Array values = Enum.GetValues(enumType);
		for (int i = 0; i < names.Length; i++)
		{
			ulong value;
			if (enumUnderlyingType == typeof(sbyte))
			{
				value = (ulong)(sbyte)values.GetValue(i);
			}
			else if (enumUnderlyingType == typeof(byte))
			{
				value = (byte)values.GetValue(i);
			}
			else if (enumUnderlyingType == typeof(short))
			{
				value = (ulong)(short)values.GetValue(i);
			}
			else if (enumUnderlyingType == typeof(ushort))
			{
				value = (ushort)values.GetValue(i);
			}
			else if (enumUnderlyingType == typeof(int))
			{
				value = (ulong)(int)values.GetValue(i);
			}
			else if (enumUnderlyingType == typeof(uint))
			{
				value = (uint)values.GetValue(i);
			}
			else if (enumUnderlyingType == typeof(long))
			{
				value = (ulong)(long)values.GetValue(i);
			}
			else
			{
				if (!(enumUnderlyingType == typeof(ulong)))
				{
					throw new Exception("Unsupported underlying type");
				}
				value = (ulong)values.GetValue(i);
			}
			if (enumType.GetField(names[i]).GetCustomAttributes(typeof(SettingsUIHiddenAttribute), inherit: false).Length == 0)
			{
				string text = enumType.Name.ToUpperInvariant() + "[" + names[i] + "]";
				if (!string.IsNullOrEmpty(prefix))
				{
					text = prefix + "." + text;
				}
				list.Add(new EnumMember(value, text));
			}
		}
		return list.ToArray();
	}

	private static bool IsShowGroupName(Setting setting, out bool showAll, out ReadOnlyCollection<string> groups)
	{
		SettingsUIShowGroupNameAttribute attribute = ReflectionUtils.GetAttribute<SettingsUIShowGroupNameAttribute>(setting.GetType().GetCustomAttributes(inherit: false));
		if (attribute != null)
		{
			showAll = attribute.showAll;
			groups = attribute.groups;
			return true;
		}
		showAll = false;
		groups = null;
		return false;
	}

	private static Func<bool> GetWarningGetter(Setting setting)
	{
		SettingsUIPageWarningAttribute attribute = ReflectionUtils.GetAttribute<SettingsUIPageWarningAttribute>(setting.GetType().GetCustomAttributes(inherit: false));
		if (attribute != null && TryGetAction(setting, attribute.checkType, attribute.checkMethod, out Func<bool> action))
		{
			return action;
		}
		return null;
	}

	private static Dictionary<string, Func<bool>> GetTabWarningGetters(Setting setting)
	{
		Dictionary<string, Func<bool>> dictionary = new Dictionary<string, Func<bool>>();
		foreach (SettingsUITabWarningAttribute attribute in ReflectionUtils.GetAttributes<SettingsUITabWarningAttribute>(setting.GetType().GetCustomAttributes(inherit: false)))
		{
			if (!string.IsNullOrEmpty(attribute.tab) && TryGetAction(setting, attribute.checkType, attribute.checkMethod, out Func<bool> action))
			{
				dictionary.TryAdd(attribute.tab, action);
			}
		}
		return dictionary;
	}

	public static SettingPageData FillSettingsPage(Setting setting, string id, bool addPrefix)
	{
		s_ButtonGroups.Clear();
		if (setting == null)
		{
			return null;
		}
		SettingPageData settingPageData = new SettingPageData(id, addPrefix);
		if (IsShowGroupName(setting, out var showAll, out var groups))
		{
			if (showAll)
			{
				settingPageData.showAllGroupNames = true;
			}
			else
			{
				foreach (string item in groups)
				{
					settingPageData.AddGroupToShowName(item);
				}
			}
		}
		settingPageData.warningGetter = GetWarningGetter(setting);
		settingPageData.tabWarningGetters = GetTabWarningGetters(setting);
		FillSettingsPage(settingPageData, setting);
		s_ButtonGroups.Clear();
		return settingPageData;
	}

	public static void FillSettingsPage(SettingPageData pageData, Setting setting)
	{
		if (setting.GetType().TryGetAttribute<SettingsUITabOrderAttribute>(out var attribute))
		{
			if (TryGetAction(setting, attribute.checkType, attribute.checkMethod, out Func<string[]> action))
			{
				string[] array = action();
				foreach (string tab in array)
				{
					pageData.AddTab(tab);
				}
			}
			else
			{
				foreach (string tab2 in attribute.tabs)
				{
					pageData.AddTab(tab2);
				}
			}
		}
		if (setting.GetType().TryGetAttribute<SettingsUIGroupOrderAttribute>(out var attribute2))
		{
			if (TryGetAction(setting, attribute2.checkType, attribute2.checkMethod, out Func<string[]> action2))
			{
				string[] array = action2();
				foreach (string text in array)
				{
					pageData.AddGroup(text);
				}
			}
			else
			{
				foreach (string group in attribute2.groups)
				{
					pageData.AddGroup(group);
				}
			}
		}
		PropertyInfo[] properties = setting.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public);
		for (int i = 0; i < properties.Length; i++)
		{
			ProxyProperty property = new ProxyProperty(properties[i]);
			if (!IsSupportedOnPlatform(property) || IsHidden(property) || IsDeveloperOnly(property))
			{
				continue;
			}
			WidgetType widgetType = GetWidgetType(property);
			if (widgetType == WidgetType.None)
			{
				continue;
			}
			foreach (SectionInfo value in GetSections(property).Values)
			{
				SettingItemData settingItemData = ((widgetType != WidgetType.MultilineText) ? new SettingItemData(widgetType, setting, property, pageData.prefix) : new MultilineTextSettingItemData(setting, property, pageData.prefix));
				SettingItemData settingItemData2 = settingItemData;
				settingItemData2.simpleGroup = value.m_SimpleGroup;
				settingItemData2.advancedGroup = value.m_AdvancedGroup;
				pageData[value.m_Tab].AddItem(settingItemData2);
				pageData.AddGroup(settingItemData2.simpleGroup);
				pageData.AddGroup(settingItemData2.advancedGroup);
			}
		}
	}

	public static WidgetType GetWidgetType(IProxyProperty property)
	{
		if (property.propertyType == typeof(bool))
		{
			if (property.HasAttribute<SettingsUIButtonAttribute>())
			{
				if (property.HasAttribute<SettingsUIConfirmationAttribute>())
				{
					return WidgetType.BoolButtonWithConfirmation;
				}
				return WidgetType.BoolButton;
			}
			if (property.canRead && property.canWrite)
			{
				return WidgetType.BoolToggle;
			}
			if (!property.canRead && property.canWrite)
			{
				return WidgetType.BoolButton;
			}
			return WidgetType.None;
		}
		if (property.propertyType == typeof(int))
		{
			if (property.HasAttribute<SettingsUIDropdownAttribute>())
			{
				return WidgetType.IntDropdown;
			}
			if (property.HasAttribute<SettingsUISliderAttribute>())
			{
				return WidgetType.IntSlider;
			}
			return WidgetType.None;
		}
		if (property.propertyType == typeof(float))
		{
			if (property.HasAttribute<SettingsUISliderAttribute>())
			{
				return WidgetType.FloatSlider;
			}
			return WidgetType.None;
		}
		if (property.propertyType == typeof(string))
		{
			if (property.canRead && property.canWrite)
			{
				if (property.HasAttribute<SettingsUITextInputAttribute>())
				{
					return WidgetType.StringTextInput;
				}
				if (property.HasAttribute<SettingsUIDropdownAttribute>())
				{
					return WidgetType.StringDropdown;
				}
				if (property.HasAttribute<SettingsUIDirectoryPickerAttribute>())
				{
					return WidgetType.DirectoryPicker;
				}
				return WidgetType.None;
			}
			if (property.canRead && !property.canWrite)
			{
				if (property.HasAttribute<SettingsUIMultilineTextAttribute>())
				{
					return WidgetType.MultilineText;
				}
				return WidgetType.StringField;
			}
			return WidgetType.None;
		}
		if (property.propertyType == typeof(LocalizedString))
		{
			if (property.canRead && !property.canWrite)
			{
				return WidgetType.LocalizedStringField;
			}
			return WidgetType.None;
		}
		if (property.propertyType == typeof(ProxyBinding))
		{
			return WidgetType.KeyBinding;
		}
		if (property.propertyType.IsEnum)
		{
			if (property.HasAttribute<SettingsUIDropdownAttribute>())
			{
				return WidgetType.AdvancedEnumDropdown;
			}
			return WidgetType.EnumDropdown;
		}
		if (property.HasAttribute<SettingsUIDropdownAttribute>())
		{
			return WidgetType.CustomDropdown;
		}
		return WidgetType.None;
	}

	public static LocalizedString GetConfirmationMessage(SettingItemData itemData)
	{
		SettingsUIConfirmationAttribute attribute = itemData.property.GetAttribute<SettingsUIConfirmationAttribute>();
		if (attribute != null)
		{
			if (!string.IsNullOrEmpty(attribute.confirmMessageId))
			{
				return LocalizedString.IdWithFallback("Options.WARNING[" + attribute.confirmMessageId + "]", attribute.confirmMessageValue);
			}
			if (!string.IsNullOrEmpty(attribute.confirmMessageValue))
			{
				return LocalizedString.Value(attribute.confirmMessageValue);
			}
		}
		string text = itemData.property.declaringType.Name + "." + itemData.property.name;
		if (!string.IsNullOrEmpty(itemData.prefix))
		{
			text = itemData.prefix + "." + text;
		}
		text = "Options.WARNING[" + text + "]";
		return LocalizedString.Id(text);
	}

	public static IWidget AddBoolButtonWithConfirmationProperty(SettingItemData itemData)
	{
		ButtonWithConfirmation item = new ButtonWithConfirmation
		{
			path = itemData.path,
			displayName = itemData.displayName,
			description = itemData.description,
			displayNameAction = itemData.dispayNameAction,
			descriptionAction = itemData.descriptionAction,
			action = delegate
			{
				itemData.property.SetValue(itemData.setting, true);
			},
			disabled = itemData.disableAction,
			hidden = itemData.hideAction,
			confirmationMessage = GetConfirmationMessage(itemData)
		};
		if (GetButtonsGroup(itemData.property.GetAttribute<SettingsUIButtonGroupAttribute>()?.name ?? (itemData.property.declaringType.Name + "." + itemData.property.name + "_ButtonGroup"), out var buttons, item))
		{
			return buttons;
		}
		return null;
	}

	public static IWidget AddBoolToggleProperty(SettingItemData itemData)
	{
		if (!itemData.property.canRead || !itemData.property.canWrite)
		{
			return null;
		}
		Action<bool> setterAction = itemData.setterAction as Action<bool>;
		return new ToggleField
		{
			path = itemData.path,
			displayName = itemData.displayName,
			description = itemData.description,
			displayNameAction = itemData.dispayNameAction,
			descriptionAction = itemData.descriptionAction,
			warningAction = itemData.warningAction,
			accessor = new DelegateAccessor<bool>(() => (bool)itemData.property.GetValue(itemData.setting), delegate(bool value)
			{
				setterAction?.Invoke(value);
				itemData.property.SetValue(itemData.setting, value);
				itemData.setting.ApplyAndSave();
			}),
			disabled = itemData.disableAction,
			hidden = itemData.hideAction
		};
	}

	public static IWidget AddBoolButtonProperty(SettingItemData itemData)
	{
		if (itemData.property.canRead || !itemData.property.canWrite)
		{
			return null;
		}
		Button item = new Button
		{
			path = itemData.path,
			displayName = itemData.displayName,
			description = itemData.description,
			displayNameAction = itemData.dispayNameAction,
			descriptionAction = itemData.descriptionAction,
			action = delegate
			{
				itemData.property.SetValue(itemData.setting, true);
			},
			disabled = itemData.disableAction,
			hidden = itemData.hideAction
		};
		if (GetButtonsGroup(itemData.property.GetAttribute<SettingsUIButtonGroupAttribute>()?.name ?? (itemData.property.declaringType.Name + "." + itemData.property.name + "_ButtonGroup"), out var buttons, item))
		{
			return buttons;
		}
		return null;
	}

	public static IWidget AddIntDropdownProperty(SettingItemData itemData)
	{
		if (itemData.property.GetAttribute<SettingsUIDropdownAttribute>() == null)
		{
			return null;
		}
		Action<int> setterAction = itemData.setterAction as Action<int>;
		return new DropdownField<int>
		{
			path = itemData.path,
			displayName = itemData.displayName,
			description = itemData.description,
			displayNameAction = itemData.dispayNameAction,
			descriptionAction = itemData.descriptionAction,
			warningAction = itemData.warningAction,
			accessor = new DelegateAccessor<int>(() => (int)itemData.property.GetValue(itemData.setting), delegate(int value)
			{
				setterAction?.Invoke(value);
				itemData.property.SetValue(itemData.setting, value);
				itemData.setting.ApplyAndSave();
			}),
			itemsAccessor = GetDropdownItemAccessor<int>(itemData.property, itemData.setting),
			itemsVersion = itemData.valueVersionAction,
			disabled = itemData.disableAction,
			hidden = itemData.hideAction
		};
	}

	public static IWidget AddIntSliderProperty(SettingItemData itemData)
	{
		SettingsUISliderAttribute sliderAttribute = itemData.property.GetAttribute<SettingsUISliderAttribute>();
		if (sliderAttribute == null)
		{
			return null;
		}
		Action<int> setterAction = itemData.setterAction as Action<int>;
		IntSliderField intSliderField = new IntSliderField
		{
			path = itemData.path,
			displayName = itemData.displayName,
			description = itemData.description,
			displayNameAction = itemData.dispayNameAction,
			descriptionAction = itemData.descriptionAction,
			warningAction = itemData.warningAction,
			min = (int)sliderAttribute.min,
			max = (int)sliderAttribute.max,
			step = (int)sliderAttribute.step,
			unit = sliderAttribute.unit,
			scaleDragVolume = sliderAttribute.scaleDragVolume,
			updateOnDragEnd = sliderAttribute.updateOnDragEnd,
			accessor = new DelegateAccessor<int>(() => (int)itemData.property.GetValue(itemData.setting) * (int)sliderAttribute.scalarMultiplier, delegate(int value)
			{
				setterAction?.Invoke(value);
				itemData.property.SetValue(itemData.setting, Mathf.RoundToInt((float)value / sliderAttribute.scalarMultiplier));
				itemData.setting.ApplyAndSave();
			}),
			disabled = itemData.disableAction,
			hidden = itemData.hideAction
		};
		SettingsUICustomFormatAttribute attribute = itemData.property.GetAttribute<SettingsUICustomFormatAttribute>();
		if (attribute != null)
		{
			intSliderField.unit = "custom";
			intSliderField.separateThousands = attribute.separateThousands;
			intSliderField.signed = attribute.signed;
		}
		return intSliderField;
	}

	public static IWidget AddFloatSliderProperty(SettingItemData itemData)
	{
		SettingsUISliderAttribute attribute = itemData.property.GetAttribute<SettingsUISliderAttribute>();
		Action<float> setterAction = itemData.setterAction as Action<float>;
		FloatSliderField floatSliderField = new FloatSliderField
		{
			path = itemData.path,
			displayName = itemData.displayName,
			description = itemData.description,
			displayNameAction = itemData.dispayNameAction,
			descriptionAction = itemData.descriptionAction,
			warningAction = itemData.warningAction,
			min = attribute.min,
			max = attribute.max,
			step = attribute.step,
			unit = attribute.unit,
			scaleDragVolume = attribute.scaleDragVolume,
			updateOnDragEnd = attribute.updateOnDragEnd,
			accessor = new DelegateAccessor<double>(() => (float)itemData.property.GetValue(itemData.setting) * (float)(int)attribute.scalarMultiplier, delegate(double value)
			{
				setterAction?.Invoke((float)value);
				itemData.property.SetValue(itemData.setting, (float)(value / (double)attribute.scalarMultiplier));
				itemData.setting.ApplyAndSave();
			}),
			disabled = itemData.disableAction,
			hidden = itemData.hideAction
		};
		SettingsUICustomFormatAttribute attribute2 = itemData.property.GetAttribute<SettingsUICustomFormatAttribute>();
		if (attribute2 != null)
		{
			floatSliderField.unit = "custom";
			floatSliderField.fractionDigits = Math.Max(attribute2.fractionDigits, 0);
			floatSliderField.separateThousands = attribute2.separateThousands;
			floatSliderField.maxValueWithFraction = attribute2.maxValueWithFraction;
			floatSliderField.signed = attribute2.signed;
		}
		return floatSliderField;
	}

	public static IWidget AddStringTextInputProperty(SettingItemData itemData)
	{
		Action<string> setterAction = itemData.setterAction as Action<string>;
		return new StringInputField
		{
			path = itemData.path,
			displayName = itemData.displayName,
			description = itemData.description,
			displayNameAction = itemData.dispayNameAction,
			descriptionAction = itemData.descriptionAction,
			warningAction = itemData.warningAction,
			accessor = new DelegateAccessor<string>(() => (string)itemData.property.GetValue(itemData.setting), delegate(string value)
			{
				setterAction?.Invoke(value);
				itemData.property.SetValue(itemData.setting, value);
				itemData.setting.ApplyAndSave();
			}),
			disabled = itemData.disableAction,
			hidden = itemData.hideAction
		};
	}

	public static IWidget AddStringDropdownProperty(SettingItemData itemData)
	{
		if (itemData.property.GetAttribute<SettingsUIDropdownAttribute>() == null)
		{
			return null;
		}
		Action<string> setterAction = itemData.setterAction as Action<string>;
		return new DropdownField<string>
		{
			path = itemData.path,
			displayName = itemData.displayName,
			description = itemData.description,
			displayNameAction = itemData.dispayNameAction,
			descriptionAction = itemData.descriptionAction,
			warningAction = itemData.warningAction,
			accessor = new DelegateAccessor<string>(() => (string)itemData.property.GetValue(itemData.setting), delegate(string value)
			{
				setterAction?.Invoke(value);
				itemData.property.SetValue(itemData.setting, value);
				itemData.setting.ApplyAndSave();
			}),
			itemsAccessor = GetDropdownItemAccessor<string>(itemData.property, itemData.setting),
			itemsVersion = itemData.valueVersionAction,
			disabled = itemData.disableAction,
			hidden = itemData.hideAction
		};
	}

	public static IWidget AddStringFieldProperty(SettingItemData itemData)
	{
		return new LocalizedValueField
		{
			path = itemData.path,
			displayName = itemData.displayName,
			description = itemData.description,
			displayNameAction = itemData.dispayNameAction,
			descriptionAction = itemData.descriptionAction,
			warningAction = itemData.warningAction,
			accessor = new DelegateAccessor<LocalizedString>(() => LocalizedString.Value((string)itemData.property.GetValue(itemData.setting)), delegate
			{
			}),
			valueVersion = itemData.valueVersionAction,
			disabled = itemData.disableAction,
			hidden = itemData.hideAction
		};
	}

	public static IWidget AddLocalizedStringFieldProperty(SettingItemData itemData)
	{
		return new LocalizedValueField
		{
			path = itemData.path,
			displayName = itemData.displayName,
			description = itemData.description,
			displayNameAction = itemData.dispayNameAction,
			descriptionAction = itemData.descriptionAction,
			warningAction = itemData.warningAction,
			accessor = new DelegateAccessor<LocalizedString>(() => (LocalizedString)itemData.property.GetValue(itemData.setting), delegate
			{
			}),
			valueVersion = itemData.valueVersionAction,
			disabled = itemData.disableAction,
			hidden = itemData.hideAction
		};
	}

	public static IWidget AddEnumDropdownProperty(SettingItemData itemData)
	{
		Action<int> customSetterAction = itemData.setterAction as Action<int>;
		return new DropdownField<int>
		{
			path = itemData.path,
			displayName = itemData.displayName,
			description = itemData.description,
			displayNameAction = itemData.dispayNameAction,
			descriptionAction = itemData.descriptionAction,
			warningAction = itemData.warningAction,
			accessor = new DelegateAccessor<int>(() => (int)itemData.property.GetValue(itemData.setting), delegate(int value)
			{
				customSetterAction?.Invoke(value);
				itemData.property.SetValue(itemData.setting, value);
				itemData.setting.ApplyAndSave();
			}),
			itemsAccessor = GetDropdownItemAccessor<int>(itemData.property, itemData.setting),
			itemsVersion = itemData.valueVersionAction,
			disabled = itemData.disableAction,
			hidden = itemData.hideAction
		};
	}

	public static IWidget AddEnumSimpleProperty(SettingItemData itemData)
	{
		Func<ulong> enumGetter = GetEnumGetter(itemData);
		Action<ulong> setter = GetEnumSetter(itemData);
		Action<ulong> customSetterAction = GetEnumCustomSetterAction(itemData);
		return new EnumField
		{
			path = itemData.path,
			displayName = itemData.displayName,
			description = itemData.description,
			displayNameAction = itemData.dispayNameAction,
			descriptionAction = itemData.descriptionAction,
			warningAction = itemData.warningAction,
			accessor = new DelegateAccessor<ulong>(enumGetter, delegate(ulong value)
			{
				customSetterAction?.Invoke(value);
				setter(value);
				itemData.setting.ApplyAndSave();
			}),
			itemsAccessor = GetEnumMemberAccessor(itemData.property, itemData.setting, itemData.prefix),
			itemsVersion = itemData.valueVersionAction,
			disabled = itemData.disableAction,
			hidden = itemData.hideAction
		};
	}

	public static IWidget AddKeyBindingProperty(SettingItemData itemData)
	{
		Action<ProxyBinding> setterAction = itemData.setterAction as Action<ProxyBinding>;
		return new InputBindingField
		{
			path = itemData.path,
			displayName = itemData.displayName,
			description = itemData.description,
			displayNameAction = itemData.dispayNameAction,
			descriptionAction = itemData.descriptionAction,
			accessor = new DelegateAccessor<ProxyBinding>(delegate
			{
				ProxyBinding binding = (ProxyBinding)itemData.property.GetValue(itemData.setting);
				ProxyBinding binding2 = InputManager.instance.GetOrCreateBindingWatcher(binding).binding;
				binding2.alies = binding.alies;
				return binding2;
			}, delegate(ProxyBinding value)
			{
				if (InputManager.instance.SetBinding(value, out var result))
				{
					setterAction?.Invoke(result);
					itemData.property.SetValue(itemData.setting, result);
					itemData.setting.ApplyAndSave();
				}
			}),
			valueVersion = (itemData.valueVersionAction ?? new Func<int>(GetValueVersion)),
			disabled = itemData.disableAction,
			hidden = itemData.hideAction
		};
		static int GetValueVersion()
		{
			return InputManager.instance.actionVersion;
		}
	}

	public static IWidget AddDirectoryPickerBindingProperty(SettingItemData itemData)
	{
		Action<string> setterAction = itemData.setterAction as Action<string>;
		return new DirectoryPickerField
		{
			path = itemData.path,
			displayName = itemData.displayName,
			displayNameAction = itemData.dispayNameAction,
			warningAction = itemData.warningAction,
			accessor = new DelegateAccessor<string>(() => (string)itemData.property.GetValue(itemData.setting), delegate(string value)
			{
				setterAction?.Invoke(value);
				itemData.property.SetValue(itemData.setting, value);
				itemData.setting.ApplyAndSave();
			}),
			disabled = itemData.disableAction,
			hidden = itemData.hideAction,
			action = delegate
			{
				string root = (string)itemData.property.GetValue(itemData.setting);
				(World.DefaultGameObjectInjectionWorld?.GetOrCreateSystemManaged<OptionsUISystem>()).OpenDirectoryBrowser(root, delegate(string value)
				{
					setterAction?.Invoke(value);
					itemData.property.SetValue(itemData.setting, value);
					itemData.setting.ApplyAndSave();
				});
			}
		};
	}

	public static IWidget AddCustomDropdownProperty(SettingItemData itemData)
	{
		if (itemData.property.GetAttribute<SettingsUIDropdownAttribute>() == null)
		{
			return null;
		}
		if (!typeof(IJsonWritable).IsAssignableFrom(itemData.property.propertyType))
		{
			return null;
		}
		if (!typeof(IJsonReadable).IsAssignableFrom(itemData.property.propertyType))
		{
			return null;
		}
		if (!itemData.property.propertyType.IsValueType && itemData.property.propertyType.GetConstructor(Type.EmptyTypes) == null)
		{
			return null;
		}
		return (IWidget)s_CustomDropdownMethodInfo.MakeGenericMethod(itemData.property.propertyType).Invoke(null, new object[1] { itemData });
	}

	public static IWidget AddCustomDropdownPropertyGeneric<T>(SettingItemData itemData) where T : IJsonWritable, IJsonReadable, new()
	{
		Action<T> setterAction = itemData.setterAction as Action<T>;
		return new DropdownField<T>
		{
			path = itemData.path,
			displayName = itemData.displayName,
			description = itemData.description,
			displayNameAction = itemData.dispayNameAction,
			descriptionAction = itemData.descriptionAction,
			warningAction = itemData.warningAction,
			accessor = new DelegateAccessor<T>(() => (T)itemData.property.GetValue(itemData.setting), delegate(T value)
			{
				setterAction?.Invoke(value);
				itemData.property.SetValue(itemData.setting, value);
				itemData.setting.ApplyAndSave();
			}),
			valueWriter = new ValueWriter<T>(),
			valueReader = new ValueReader<T>(),
			itemsVersion = itemData.valueVersionAction,
			itemsAccessor = GetDropdownItemAccessor<T>(itemData.property, itemData.setting),
			disabled = itemData.disableAction,
			hidden = itemData.hideAction
		};
	}

	private static Func<ulong> GetEnumGetter(SettingItemData itemData)
	{
		Type propertyType = itemData.property.propertyType;
		if (!propertyType.IsEnum)
		{
			throw new ArgumentException("Property type is not an enum");
		}
		Type enumUnderlyingType = propertyType.GetEnumUnderlyingType();
		if (enumUnderlyingType == typeof(sbyte))
		{
			return () => (ulong)(sbyte)itemData.property.GetValue(itemData.setting);
		}
		if (enumUnderlyingType == typeof(byte))
		{
			return () => (byte)itemData.property.GetValue(itemData.setting);
		}
		if (enumUnderlyingType == typeof(short))
		{
			return () => (ulong)(short)itemData.property.GetValue(itemData.setting);
		}
		if (enumUnderlyingType == typeof(ushort))
		{
			return () => (ushort)itemData.property.GetValue(itemData.setting);
		}
		if (enumUnderlyingType == typeof(int))
		{
			return () => (ulong)(int)itemData.property.GetValue(itemData.setting);
		}
		if (enumUnderlyingType == typeof(uint))
		{
			return () => (uint)itemData.property.GetValue(itemData.setting);
		}
		if (enumUnderlyingType == typeof(long))
		{
			return () => (ulong)(long)itemData.property.GetValue(itemData.setting);
		}
		if (enumUnderlyingType == typeof(ulong))
		{
			return () => (ulong)itemData.property.GetValue(itemData.setting);
		}
		throw new Exception("Unsupported underlying type");
	}

	private static Action<ulong> GetEnumSetter(SettingItemData itemData)
	{
		Type propertyType = itemData.property.propertyType;
		if (!propertyType.IsEnum)
		{
			throw new ArgumentException("Property type is not an enum");
		}
		Type enumUnderlyingType = propertyType.GetEnumUnderlyingType();
		if (enumUnderlyingType == typeof(sbyte))
		{
			return delegate(ulong value)
			{
				itemData.property.SetValue(itemData.setting, (sbyte)value);
			};
		}
		if (enumUnderlyingType == typeof(byte))
		{
			return delegate(ulong value)
			{
				itemData.property.SetValue(itemData.setting, (byte)value);
			};
		}
		if (enumUnderlyingType == typeof(short))
		{
			return delegate(ulong value)
			{
				itemData.property.SetValue(itemData.setting, (short)value);
			};
		}
		if (enumUnderlyingType == typeof(ushort))
		{
			return delegate(ulong value)
			{
				itemData.property.SetValue(itemData.setting, (ushort)value);
			};
		}
		if (enumUnderlyingType == typeof(int))
		{
			return delegate(ulong value)
			{
				itemData.property.SetValue(itemData.setting, (int)value);
			};
		}
		if (enumUnderlyingType == typeof(uint))
		{
			return delegate(ulong value)
			{
				itemData.property.SetValue(itemData.setting, (uint)value);
			};
		}
		if (enumUnderlyingType == typeof(long))
		{
			return delegate(ulong value)
			{
				itemData.property.SetValue(itemData.setting, (long)value);
			};
		}
		if (enumUnderlyingType == typeof(ulong))
		{
			return delegate(ulong value)
			{
				itemData.property.SetValue(itemData.setting, value);
			};
		}
		throw new Exception("Unsupported underlying type");
	}

	private static Action<ulong> GetEnumCustomSetterAction(SettingItemData itemData)
	{
		if ((object)itemData.setterAction == null)
		{
			return null;
		}
		Delegate setterAction = itemData.setterAction;
		Action<int> intSetter = setterAction as Action<int>;
		if (intSetter != null)
		{
			return delegate(ulong value)
			{
				intSetter((int)value);
			};
		}
		Type propertyType = itemData.property.propertyType;
		if (!propertyType.IsEnum)
		{
			return null;
		}
		Type type = typeof(Action<>).MakeGenericType(propertyType);
		if (itemData.setterAction.GetType() == type)
		{
			return (Action<ulong>)s_EnumSetterMethodInfo.MakeGenericMethod(itemData.property.propertyType).Invoke(null, new object[1] { itemData });
		}
		Type enumUnderlyingType = propertyType.GetEnumUnderlyingType();
		if (enumUnderlyingType == typeof(sbyte))
		{
			setterAction = itemData.setterAction;
			Action<sbyte> setter = setterAction as Action<sbyte>;
			if (setter != null)
			{
				return delegate(ulong value)
				{
					setter((sbyte)value);
				};
			}
		}
		else if (enumUnderlyingType == typeof(byte))
		{
			setterAction = itemData.setterAction;
			Action<byte> setter2 = setterAction as Action<byte>;
			if (setter2 != null)
			{
				return delegate(ulong value)
				{
					setter2((byte)value);
				};
			}
		}
		else if (enumUnderlyingType == typeof(short))
		{
			setterAction = itemData.setterAction;
			Action<short> setter3 = setterAction as Action<short>;
			if (setter3 != null)
			{
				return delegate(ulong value)
				{
					setter3((short)value);
				};
			}
		}
		else if (enumUnderlyingType == typeof(ushort))
		{
			setterAction = itemData.setterAction;
			Action<ushort> setter4 = setterAction as Action<ushort>;
			if (setter4 != null)
			{
				return delegate(ulong value)
				{
					setter4((ushort)value);
				};
			}
		}
		else if (enumUnderlyingType == typeof(int))
		{
			setterAction = itemData.setterAction;
			Action<int> setter5 = setterAction as Action<int>;
			if (setter5 != null)
			{
				return delegate(ulong value)
				{
					setter5((int)value);
				};
			}
		}
		else if (enumUnderlyingType == typeof(uint))
		{
			setterAction = itemData.setterAction;
			Action<uint> setter6 = setterAction as Action<uint>;
			if (setter6 != null)
			{
				return delegate(ulong value)
				{
					setter6((uint)value);
				};
			}
		}
		else if (enumUnderlyingType == typeof(long))
		{
			setterAction = itemData.setterAction;
			Action<long> setter7 = setterAction as Action<long>;
			if (setter7 != null)
			{
				return delegate(ulong value)
				{
					setter7((long)value);
				};
			}
		}
		else if (enumUnderlyingType == typeof(ulong) && itemData.setterAction is Action<ulong> result)
		{
			return result;
		}
		return null;
	}

	private static Action<ulong> GetSetterActionGeneric<T>(SettingItemData itemData) where T : Enum
	{
		Delegate setterAction = itemData.setterAction;
		Action<T> setter = setterAction as Action<T>;
		if (setter != null)
		{
			Type enumUnderlyingType = typeof(T).GetEnumUnderlyingType();
			if (enumUnderlyingType == typeof(sbyte))
			{
				return delegate(ulong value)
				{
					setter((T)Enum.ToObject(typeof(T), (sbyte)value));
				};
			}
			if (enumUnderlyingType == typeof(byte))
			{
				return delegate(ulong value)
				{
					setter((T)Enum.ToObject(typeof(T), (byte)value));
				};
			}
			if (enumUnderlyingType == typeof(short))
			{
				return delegate(ulong value)
				{
					setter((T)Enum.ToObject(typeof(T), (short)value));
				};
			}
			if (enumUnderlyingType == typeof(ushort))
			{
				return delegate(ulong value)
				{
					setter((T)Enum.ToObject(typeof(T), (ushort)value));
				};
			}
			if (enumUnderlyingType == typeof(int))
			{
				return delegate(ulong value)
				{
					setter((T)Enum.ToObject(typeof(T), (int)value));
				};
			}
			if (enumUnderlyingType == typeof(uint))
			{
				return delegate(ulong value)
				{
					setter((T)Enum.ToObject(typeof(T), (uint)value));
				};
			}
			if (enumUnderlyingType == typeof(long))
			{
				return delegate(ulong value)
				{
					setter((T)Enum.ToObject(typeof(T), (long)value));
				};
			}
			if (enumUnderlyingType == typeof(ulong))
			{
				return delegate(ulong value)
				{
					setter((T)Enum.ToObject(typeof(T), value));
				};
			}
		}
		return null;
	}
}
