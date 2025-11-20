using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Colossal.Json;
using Colossal.Reflection;
using Game.Input;
using Game.Settings;
using UnityEngine.InputSystem.Utilities;

namespace Game.Modding;

public abstract class ModSetting : Setting
{
	private PropertyInfo[] m_keyBindingProperties;

	internal static Dictionary<string, ModSetting> instances { get; } = new Dictionary<string, ModSetting>();

	internal IMod mod { get; }

	protected internal sealed override bool builtIn => false;

	[SettingsUIHidden]
	public string id { get; }

	[SettingsUIHidden]
	public string name { get; }

	[SettingsUIHidden]
	public bool keyBindingRegistered { get; private set; }

	private PropertyInfo[] keyBindingProperties => m_keyBindingProperties ?? (m_keyBindingProperties = (from p in GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
		where p.CanRead && p.CanWrite && p.PropertyType == typeof(ProxyBinding)
		select p).ToArray());

	public ModSetting(IMod mod)
	{
		Type type = mod.GetType();
		id = type.Assembly.GetName().Name + "." + type.Namespace + "." + type.Name;
		name = GetType().Name;
		this.mod = mod;
		instances[id] = this;
		InitializeKeyBindings();
	}

	public void RegisterInOptionsUI()
	{
		RegisterInOptionsUI(id, addPrefix: true);
	}

	public void UnregisterInOptionsUI()
	{
		Setting.UnregisterInOptionsUI(id);
	}

	private void InitializeKeyBindings()
	{
		PropertyInfo[] array = keyBindingProperties;
		foreach (PropertyInfo propertyInfo in array)
		{
			ProxyBinding proxyBinding = GenerateBinding(propertyInfo);
			propertyInfo.SetValue(this, proxyBinding);
		}
	}

	private ProxyBinding GenerateBinding(PropertyInfo property)
	{
		string actionName;
		InputManager.DeviceType device;
		ActionType type;
		ActionComponent component;
		string control;
		IEnumerable<string> modifierControls;
		SettingsUIGamepadBindingAttribute attribute2;
		SettingsUIMouseBindingAttribute attribute3;
		if (((MemberInfo)property).TryGetAttribute(out SettingsUIKeyboardBindingAttribute attribute, inherit: false))
		{
			actionName = attribute.actionName ?? property.Name;
			device = attribute.device;
			type = attribute.type;
			component = attribute.component;
			control = attribute.control;
			modifierControls = attribute.modifierControls;
		}
		else if (((MemberInfo)property).TryGetAttribute(out attribute2, inherit: false))
		{
			actionName = attribute2.actionName ?? property.Name;
			device = attribute2.device;
			type = attribute2.type;
			component = attribute2.component;
			control = attribute2.control;
			modifierControls = attribute2.modifierControls;
		}
		else if (((MemberInfo)property).TryGetAttribute(out attribute3, inherit: false))
		{
			actionName = attribute3.actionName ?? property.Name;
			device = attribute3.device;
			type = attribute3.type;
			component = attribute3.component;
			control = attribute3.control;
			modifierControls = attribute3.modifierControls;
		}
		else
		{
			actionName = property.Name;
			device = InputManager.DeviceType.Keyboard;
			type = ActionType.Button;
			component = ActionComponent.Press;
			control = string.Empty;
			modifierControls = Array.Empty<string>();
		}
		if (!TryGetSourceBindingForMimic(property, device, component, out var sourceBinding))
		{
			return CreateBinding(device, actionName, type, component, control, modifierControls);
		}
		return CreateMimicBinding(device, actionName, type, component, sourceBinding);
	}

	private bool TryGetSourceBindingForMimic(PropertyInfo property, InputManager.DeviceType device, ActionComponent component, out ProxyBinding sourceBinding)
	{
		sourceBinding = default(ProxyBinding);
		if (!((MemberInfo)property).TryGetAttribute(out SettingsUIBindingMimicAttribute attribute, inherit: false))
		{
			return false;
		}
		if (!InputManager.instance.TryFindAction(attribute.map, attribute.action, out var action) || !action.isBuiltIn)
		{
			return false;
		}
		if (!action.TryGetComposite(device, out var composite))
		{
			return false;
		}
		if (!composite.TryGetBinding(component, out sourceBinding))
		{
			return false;
		}
		return true;
	}

	public void RegisterKeyBindings()
	{
		if (keyBindingRegistered)
		{
			return;
		}
		PropertyInfo[] array = keyBindingProperties;
		Dictionary<string, (ProxyAction.Info, List<PropertyInfo>)> dictionary = new Dictionary<string, (ProxyAction.Info, List<PropertyInfo>)>();
		Dictionary<(string, InputManager.DeviceType), SettingsUIInputActionAttribute> dictionary2 = new Dictionary<(string, InputManager.DeviceType), SettingsUIInputActionAttribute>();
		foreach (SettingsUIInputActionAttribute attribute in ReflectionUtils.GetAttributes<SettingsUIInputActionAttribute>(GetType().GetCustomAttributes(inherit: false)))
		{
			dictionary2.TryAdd((attribute.name, attribute.device), attribute);
		}
		foreach (PropertyInfo propertyInfo in array)
		{
			ProxyBinding binding = (ProxyBinding)propertyInfo.GetValue(this);
			if (!dictionary.TryGetValue(binding.actionName, out var value))
			{
				value = (new ProxyAction.Info
				{
					m_Map = binding.mapName,
					m_Name = binding.actionName,
					m_Type = binding.component.GetActionType(),
					m_Composites = new List<ProxyComposite.Info>()
				}, new List<PropertyInfo>());
			}
			if (binding.component.GetActionType() != value.Item1.m_Type)
			{
				continue;
			}
			value.Item2.Add(propertyInfo);
			ProxyComposite.Info item = value.Item1.m_Composites.FirstOrDefault((ProxyComposite.Info info) => info.m_Device == binding.device);
			if (item.m_Source == null)
			{
				if (!InputManager.TryGetCompositeData(binding.component.GetActionType(), out var data))
				{
					continue;
				}
				CompositeInstance compositeInstance = new CompositeInstance(data.m_TypeName)
				{
					builtIn = false
				};
				if (dictionary2.TryGetValue((binding.actionName, binding.device), out var value2))
				{
					compositeInstance.rebindOptions = value2.rebindOptions;
					compositeInstance.modifierOptions = value2.modifierOptions;
					compositeInstance.developerOnly = value2.developerOnly;
					compositeInstance.mode = value2.mode;
					compositeInstance.usages = value2.usages;
					compositeInstance.interactions.AddRange(value2.interactions.Select(NameAndParameters.Parse));
					compositeInstance.processors.AddRange(value2.processors.Select(NameAndParameters.Parse));
				}
				else
				{
					compositeInstance.rebindOptions = RebindOptions.All;
					compositeInstance.modifierOptions = ModifierOptions.Allow;
				}
				item = new ProxyComposite.Info
				{
					m_Device = binding.device,
					m_Source = compositeInstance,
					m_Bindings = new List<ProxyBinding>()
				};
				value.Item1.m_Composites.Add(item);
			}
			item.m_Bindings.Add(binding);
			dictionary[binding.actionName] = value;
		}
		ProxyAction.Info[] actionsToAdd = dictionary.Values.Select<(ProxyAction.Info, List<PropertyInfo>), ProxyAction.Info>(((ProxyAction.Info actionInfo, List<PropertyInfo> properties) d) => d.actionInfo).ToArray();
		InputManager.instance.AddActions(actionsToAdd);
		PropertyInfo[] array2 = array;
		foreach (PropertyInfo property in array2)
		{
			ProxyBinding binding2 = (ProxyBinding)property.GetValue(this);
			binding2.CreateWatcher(delegate(ProxyBinding newBinding)
			{
				property.SetValue(this, newBinding);
			});
			if (TryGetSourceBindingForMimic(property, binding2.device, binding2.component, out var sourceBinding))
			{
				sourceBinding.CreateWatcher(delegate(ProxyBinding newSourceBinding)
				{
					ProxyBinding newBinding = binding2.Copy();
					newBinding.path = newSourceBinding.path;
					newBinding.modifiers = newSourceBinding.modifiers;
					InputManager.instance.SetBinding(newBinding, out var _);
				});
			}
		}
		keyBindingRegistered = true;
	}

	private ProxyBinding CreateBinding(InputManager.DeviceType device, string actionName, ActionType type, ActionComponent component, string control, IEnumerable<string> modifierControls)
	{
		if (!InputManager.TryGetCompositeData(type, out var data) || !data.TryGetData(component, out var componentData))
		{
			componentData = InputManager.CompositeComponentData.defaultData;
		}
		ProxyModifier[] array = modifierControls.Select((string modifierControl) => new ProxyModifier
		{
			m_Component = component,
			m_Name = componentData.m_ModifierName,
			m_Path = modifierControl
		}).ToArray();
		ProxyBinding result = new ProxyBinding(id, actionName, component, componentData.m_BindingName, new CompositeInstance(device.ToString()));
		result.device = device;
		result.path = control;
		result.originalPath = control;
		result.modifiers = array;
		result.originalModifiers = array;
		return result;
	}

	private ProxyBinding CreateMimicBinding(InputManager.DeviceType device, string actionName, ActionType type, ActionComponent component, ProxyBinding sourceBinding)
	{
		if (!InputManager.TryGetCompositeData(type, out var data) || !data.TryGetData(component, out var data2))
		{
			data2 = InputManager.CompositeComponentData.defaultData;
		}
		ProxyBinding result = new ProxyBinding(id, actionName, component, data2.m_BindingName, new CompositeInstance(device.ToString()));
		result.device = device;
		result.path = sourceBinding.path;
		result.originalPath = sourceBinding.originalPath;
		result.modifiers = sourceBinding.modifiers;
		result.originalModifiers = sourceBinding.modifiers;
		return result;
	}

	[AfterDecode]
	protected internal void ApplyKeyBindings()
	{
		if (keyBindingRegistered)
		{
			ProxyBinding[] newBindings = keyBindingProperties.Select((PropertyInfo p) => (ProxyBinding)p.GetValue(this)).ToArray();
			InputManager.instance.SetBindings(newBindings, out var _);
		}
	}

	protected void ResetKeyBindings()
	{
		if (keyBindingRegistered)
		{
			ProxyBinding[] newBindings = keyBindingProperties.Select(GenerateBinding).ToArray();
			InputManager.instance.SetBindings(newBindings, out var _);
			ApplyAndSave();
		}
	}

	public ProxyAction GetAction(string name)
	{
		return InputManager.instance.FindAction(id, name);
	}

	public IEnumerable<ProxyAction> GetActions()
	{
		if (!InputManager.instance.TryFindActionMap(id, out var map))
		{
			return Array.Empty<ProxyAction>();
		}
		return map.actions.Values;
	}

	public string GetSettingsLocaleID()
	{
		return "Options.SECTION[" + id + "]";
	}

	public string GetOptionLabelLocaleID(string optionName)
	{
		return "Options.OPTION[" + id + "." + name + "." + optionName + "]";
	}

	public string GetOptionDescLocaleID(string optionName)
	{
		return "Options.OPTION_DESCRIPTION[" + id + "." + name + "." + optionName + "]";
	}

	public string GetOptionWarningLocaleID(string optionName)
	{
		return "Options.WARNING[" + id + "." + name + "." + optionName + "]";
	}

	public string GetOptionTabLocaleID(string tabName)
	{
		return "Options.TAB[" + id + "." + tabName + "]";
	}

	public string GetOptionGroupLocaleID(string groupName)
	{
		return "Options.GROUP[" + id + "." + groupName + "]";
	}

	public string GetEnumValueLocaleID<T>(T value) where T : Enum
	{
		return $"Options.{id}.{typeof(T).Name.ToUpper()}[{value}]";
	}

	public string GetOptionFormatLocaleID(string optionName)
	{
		return "Options.FORMAT[" + id + "." + name + "." + optionName + "]";
	}

	public string GetBindingKeyLocaleID(string actionName)
	{
		return GetBindingKeyLocaleID(actionName, InputManager.GetBindingName(ActionComponent.Press));
	}

	public string GetBindingKeyLocaleID(string actionName, AxisComponent component)
	{
		return GetBindingKeyLocaleID(actionName, InputManager.GetBindingName((ActionComponent)component));
	}

	public string GetBindingKeyLocaleID(string actionName, Vector2Component component)
	{
		return GetBindingKeyLocaleID(actionName, InputManager.GetBindingName((ActionComponent)component));
	}

	private string GetBindingKeyLocaleID(string actionName, string componentName)
	{
		return "Options.OPTION[" + id + "/" + actionName + "/" + componentName + "]";
	}

	public string GetBindingKeyHintLocaleID(string actionName)
	{
		return "Common.ACTION[" + id + "/" + actionName + "]";
	}

	public string GetBindingMapLocaleID()
	{
		return "Options.INPUT_MAP[" + id + "]";
	}
}
