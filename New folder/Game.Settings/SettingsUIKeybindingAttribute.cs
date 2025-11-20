using System;
using System.Collections.Generic;
using Game.Input;

namespace Game.Settings;

public abstract class SettingsUIKeybindingAttribute : Attribute
{
	public readonly string actionName;

	public readonly InputManager.DeviceType device;

	public readonly ActionType type;

	public readonly ActionComponent component;

	public abstract string control { get; }

	public abstract IEnumerable<string> modifierControls { get; }

	protected SettingsUIKeybindingAttribute(string actionName, InputManager.DeviceType device, ActionType type, ActionComponent component)
	{
		this.actionName = actionName;
		this.device = device;
		this.type = type;
		this.component = component;
	}
}
