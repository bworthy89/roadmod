using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.IO.AssetDatabase;
using Game.Input;
using Game.UI.Menu;
using UnityEngine.InputSystem;

namespace Game.Settings;

[FileLocation("Settings")]
[SettingsUITabOrder(typeof(InputSettings), "GetTabOrder")]
[SettingsUIGroupOrder(new string[]
{
	"General", "Navigation", "Camera", "Tool", "Menu", "Simulation", "Toolbar", "SIP", "Tutorial", "Photo mode",
	"Editor", "Shortcuts", "Debug"
})]
[SettingsUITabWarning("Keyboard", typeof(InputSettings), "isKeyboardConflict")]
[SettingsUITabWarning("Mouse", typeof(InputSettings), "isMouseConflict")]
[SettingsUITabWarning("Gamepad", typeof(InputSettings), "isGamepadConflict")]
public class InputSettings : Setting
{
	public const string kName = "Input";

	public const string kMiscTab = "Misc";

	[SettingsUIHideByCondition(typeof(Game.Input.InputManager), "IsGamepadActive")]
	[SettingsUISection("Misc", "General")]
	public bool elevationDraggingEnabled { get; set; }

	[SettingsUISection("Mouse", "Navigation")]
	[SettingsUISlider(min = 0.1f, max = 5f, step = 0.1f, unit = "custom")]
	[SettingsUICustomFormat(fractionDigits = 1)]
	[SettingsUIHideByCondition(typeof(Game.Input.InputManager), "IsMouseConnected", true)]
	public float mouseScrollSensitivity { get; set; }

	public float finalScrollSensitivity => mouseScrollSensitivity;

	[SettingsUISection("Keyboard", "Camera")]
	[SettingsUISlider(min = 0.1f, max = 5f, step = 0.1f, unit = "custom")]
	[SettingsUICustomFormat(fractionDigits = 1)]
	[SettingsUIHideByCondition(typeof(Game.Input.InputManager), "IsKeyboardConnected", true)]
	public float keyboardMoveSensitivity { get; set; }

	[SettingsUISection("Keyboard", "Camera")]
	[SettingsUISlider(min = 0.1f, max = 5f, step = 0.1f, unit = "custom")]
	[SettingsUICustomFormat(fractionDigits = 1)]
	[SettingsUIHideByCondition(typeof(Game.Input.InputManager), "IsKeyboardConnected", true)]
	public float keyboardRotateSensitivity { get; set; }

	[SettingsUISection("Keyboard", "Camera")]
	[SettingsUISlider(min = 0.1f, max = 5f, step = 0.1f, unit = "custom")]
	[SettingsUICustomFormat(fractionDigits = 1)]
	[SettingsUIHideByCondition(typeof(Game.Input.InputManager), "IsKeyboardConnected", true)]
	public float keyboardZoomSensitivity { get; set; }

	[SettingsUISection("Mouse", "Camera")]
	[SettingsUISlider(min = 0.1f, max = 5f, step = 0.1f, unit = "custom")]
	[SettingsUICustomFormat(fractionDigits = 1)]
	[SettingsUIHideByCondition(typeof(Game.Input.InputManager), "IsMouseConnected", true)]
	public float mouseMoveSensitivity { get; set; }

	[SettingsUISection("Mouse", "Camera")]
	[SettingsUISlider(min = 0.1f, max = 5f, step = 0.1f, unit = "custom")]
	[SettingsUICustomFormat(fractionDigits = 1)]
	[SettingsUIHideByCondition(typeof(Game.Input.InputManager), "IsMouseConnected", true)]
	public float mouseRotateSensitivity { get; set; }

	[SettingsUISection("Mouse", "Camera")]
	[SettingsUISlider(min = 0.1f, max = 5f, step = 0.1f, unit = "custom")]
	[SettingsUICustomFormat(fractionDigits = 1)]
	[SettingsUIHideByCondition(typeof(Game.Input.InputManager), "IsMouseConnected", true)]
	public float mouseZoomSensitivity { get; set; }

	[SettingsUISection("Mouse", "Camera")]
	[SettingsUIHideByCondition(typeof(Game.Input.InputManager), "IsMouseConnected", true)]
	public bool mouseInvertX { get; set; }

	[SettingsUISection("Mouse", "Camera")]
	[SettingsUIHideByCondition(typeof(Game.Input.InputManager), "IsMouseConnected", true)]
	public bool mouseInvertY { get; set; }

	[SettingsUISection("Gamepad", "Camera")]
	[SettingsUISlider(min = 0.1f, max = 5f, step = 0.1f, unit = "custom")]
	[SettingsUICustomFormat(fractionDigits = 1)]
	[SettingsUIHideByCondition(typeof(Game.Input.InputManager), "IsGamepadConnected", true)]
	public float gamepadMoveSensitivity { get; set; }

	[SettingsUISection("Gamepad", "Camera")]
	[SettingsUISlider(min = 0.1f, max = 5f, step = 0.1f, unit = "custom")]
	[SettingsUICustomFormat(fractionDigits = 1)]
	[SettingsUIHideByCondition(typeof(Game.Input.InputManager), "IsGamepadConnected", true)]
	public float gamepadRotateSensitivity { get; set; }

	[SettingsUISection("Gamepad", "Camera")]
	[SettingsUISlider(min = 0.1f, max = 5f, step = 0.1f, unit = "custom")]
	[SettingsUICustomFormat(fractionDigits = 1)]
	[SettingsUIHideByCondition(typeof(Game.Input.InputManager), "IsGamepadConnected", true)]
	public float gamepadZoomSensitivity { get; set; }

	[SettingsUISection("Gamepad", "Camera")]
	[SettingsUIHideByCondition(typeof(Game.Input.InputManager), "IsGamepadConnected", true)]
	public bool gamepadInvertX { get; set; }

	[SettingsUISection("Gamepad", "Camera")]
	[SettingsUIHideByCondition(typeof(Game.Input.InputManager), "IsGamepadConnected", true)]
	public bool gamepadInvertY { get; set; }

	private bool isKeyboardConflict => (Game.Input.InputManager.instance.bindingConflicts & Game.Input.InputManager.DeviceType.Keyboard) != 0;

	private bool isMouseConflict => (Game.Input.InputManager.instance.bindingConflicts & Game.Input.InputManager.DeviceType.Mouse) != 0;

	private bool isGamepadConflict => (Game.Input.InputManager.instance.bindingConflicts & Game.Input.InputManager.DeviceType.Gamepad) != 0;

	private string[] GetTabOrder()
	{
		if (Game.Input.InputManager.instance.activeControlScheme != Game.Input.InputManager.ControlScheme.Gamepad)
		{
			return new string[4] { "Keyboard", "Mouse", "Gamepad", "Misc" };
		}
		return new string[4] { "Gamepad", "Keyboard", "Mouse", "Misc" };
	}

	public InputSettings()
	{
		SetDefaults();
	}

	public override void SetDefaults()
	{
		elevationDraggingEnabled = false;
		SetDefaultsForDevice(Game.Input.InputManager.DeviceType.Keyboard);
		SetDefaultsForDevice(Game.Input.InputManager.DeviceType.Mouse);
		SetDefaultsForDevice(Game.Input.InputManager.DeviceType.Gamepad);
	}

	private void SetDefaultsForDevice(Game.Input.InputManager.DeviceType device)
	{
		switch (device)
		{
		case Game.Input.InputManager.DeviceType.Keyboard:
			keyboardMoveSensitivity = 1f;
			keyboardZoomSensitivity = 1f;
			keyboardRotateSensitivity = 1f;
			break;
		case Game.Input.InputManager.DeviceType.Mouse:
			mouseMoveSensitivity = 1f;
			mouseRotateSensitivity = 1f;
			mouseZoomSensitivity = 1f;
			mouseInvertX = false;
			mouseInvertY = false;
			mouseScrollSensitivity = 1f;
			break;
		case Game.Input.InputManager.DeviceType.Gamepad:
			gamepadMoveSensitivity = 1f;
			gamepadZoomSensitivity = 1f;
			gamepadRotateSensitivity = 1f;
			gamepadInvertX = false;
			gamepadInvertY = false;
			break;
		case Game.Input.InputManager.DeviceType.Keyboard | Game.Input.InputManager.DeviceType.Mouse:
			break;
		}
	}

	public override AutomaticSettings.SettingPageData GetPageData(string id, bool addPrefix)
	{
		AutomaticSettings.SettingPageData pageData = base.GetPageData(id, addPrefix);
		if (InputSystem.devices.Count != 0)
		{
			if (InputSystem.devices.Any((InputDevice d) => d.added && d is Keyboard))
			{
				GetPageSection(pageData, Game.Input.InputManager.DeviceType.Keyboard);
			}
			if (InputSystem.devices.Any((InputDevice d) => d.added && d is Mouse))
			{
				GetPageSection(pageData, Game.Input.InputManager.DeviceType.Mouse);
			}
			if (InputSystem.devices.Any((InputDevice d) => d.added && d is Gamepad))
			{
				GetPageSection(pageData, Game.Input.InputManager.DeviceType.Gamepad);
			}
		}
		return pageData;
	}

	private void GetPageSection(AutomaticSettings.SettingPageData pageData, Game.Input.InputManager.DeviceType device)
	{
		AutomaticSettings.ManualProperty property = new AutomaticSettings.ManualProperty(typeof(InputSettings), typeof(bool), "resetButton")
		{
			canRead = false,
			canWrite = true,
			attributes = 
			{
				(Attribute)new SettingsUIButtonAttribute(),
				(Attribute)new SettingsUIPathAttribute(string.Format("{0}.{1}.resetbutton", "InputSettings", device)),
				(Attribute)new SettingsUIButtonGroupAttribute(string.Format("{0}.{1}.resetbutton_Group", "InputSettings", device)),
				(Attribute)new SettingsUIConfirmationAttribute(string.Format("{0}.{1}.resetbutton", "InputSettings", device)),
				(Attribute)new SettingsUIDisplayNameAttribute(string.Format("{0}.{1}.resetbutton", "InputSettings", device))
			},
			setter = delegate
			{
				Game.Input.InputManager.instance.ResetGroupBindings(device);
				SetDefaultsForDevice(device);
				ApplyAndSave();
			}
		};
		AutomaticSettings.SettingItemData item = new AutomaticSettings.SettingItemData(AutomaticSettings.WidgetType.BoolButtonWithConfirmation, this, property, pageData.prefix)
		{
			simpleGroup = "General"
		};
		pageData[device.ToString()].AddItem(item);
		pageData.AddGroup("General");
		foreach (ProxyAction action in Game.Input.InputManager.instance.actions)
		{
			foreach (var (_, proxyComposite2) in action.composites)
			{
				if (proxyComposite2.m_Device != device || !action.isBuiltIn || proxyComposite2.isDummy)
				{
					continue;
				}
				ActionComponent key;
				ProxyBinding value;
				if (!proxyComposite2.isHidden)
				{
					foreach (KeyValuePair<ActionComponent, ProxyBinding> binding2 in proxyComposite2.bindings)
					{
						binding2.Deconstruct(out key, out value);
						ProxyBinding binding = value;
						AutomaticSettings.ManualProperty property2 = new AutomaticSettings.ManualProperty(typeof(InputSettings), binding.GetType(), binding.name)
						{
							attributes = { (Attribute)new SettingsUIPathAttribute(string.Format("{0}.{1}.{2}", "InputSettings", device, binding.title)) },
							getter = (object _) => binding
						};
						AutomaticSettings.SettingItemData settingItemData = new AutomaticSettings.SettingItemData(AutomaticSettings.WidgetType.KeyBinding, this, property2, pageData.prefix)
						{
							simpleGroup = binding.GetOptionsGroup()
						};
						pageData[device.ToString()].AddItem(settingItemData);
						pageData.AddGroup(settingItemData.simpleGroup);
						pageData.AddGroupToShowName(settingItemData.simpleGroup);
					}
				}
				foreach (UIBaseInputAction uIAlias in action.m_UIAliases)
				{
					if (!uIAlias.showInOptions)
					{
						continue;
					}
					foreach (UIInputActionPart actionPart in uIAlias.actionParts)
					{
						if ((actionPart.m_Mask & device) == 0)
						{
							continue;
						}
						foreach (KeyValuePair<ActionComponent, ProxyBinding> binding3 in proxyComposite2.bindings)
						{
							binding3.Deconstruct(out key, out value);
							ProxyBinding proxyBinding = value;
							if (actionPart.m_Transform == UIBaseInputAction.Transform.None || (proxyBinding.component.ToTransform() & actionPart.m_Transform) != UIBaseInputAction.Transform.None)
							{
								ProxyBinding aliasBinding = proxyBinding.Copy();
								aliasBinding.alies = uIAlias;
								AutomaticSettings.ManualProperty property3 = new AutomaticSettings.ManualProperty(typeof(InputSettings), aliasBinding.GetType(), aliasBinding.name)
								{
									attributes = { (Attribute)new SettingsUIPathAttribute(string.Format("{0}.{1}.{2}", "InputSettings", device, aliasBinding.title)) },
									getter = (object _) => aliasBinding
								};
								AutomaticSettings.SettingItemData settingItemData2 = new AutomaticSettings.SettingItemData(AutomaticSettings.WidgetType.KeyBinding, this, property3, pageData.prefix)
								{
									simpleGroup = aliasBinding.GetOptionsGroup()
								};
								pageData[device.ToString()].AddItem(settingItemData2);
								pageData.AddGroup(settingItemData2.simpleGroup);
								pageData.AddGroupToShowName(settingItemData2.simpleGroup);
							}
						}
					}
				}
			}
		}
	}
}
