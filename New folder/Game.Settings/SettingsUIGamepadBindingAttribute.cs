using System;
using System.Collections.Generic;
using Game.Input;
using UnityEngine.InputSystem.LowLevel;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Property)]
public class SettingsUIGamepadBindingAttribute : SettingsUIKeybindingAttribute
{
	public readonly BindingGamepad defaultKey;

	public readonly bool leftStick;

	public readonly bool rightStick;

	public override string control => defaultKey switch
	{
		BindingGamepad.None => string.Empty, 
		BindingGamepad.DpadUp => "<Gamepad>/dpad/up", 
		BindingGamepad.DpadDown => "<Gamepad>/dpad/down", 
		BindingGamepad.DpadLeft => "<Gamepad>/dpad/left", 
		BindingGamepad.DpadRight => "<Gamepad>/dpad/right", 
		BindingGamepad.North => "<Gamepad>/buttonNorth", 
		BindingGamepad.East => "<Gamepad>/buttonEast", 
		BindingGamepad.South => "<Gamepad>/buttonSouth", 
		BindingGamepad.West => "<Gamepad>/buttonWest", 
		BindingGamepad.LeftShoulder => "<Gamepad>/leftShoulder", 
		BindingGamepad.RightShoulder => "<Gamepad>/rightShoulder", 
		BindingGamepad.Start => "<Gamepad>/start", 
		BindingGamepad.Select => "<Gamepad>/select", 
		BindingGamepad.LeftTrigger => "<Gamepad>/leftTrigger", 
		BindingGamepad.RightTrigger => "<Gamepad>/rightTrigger", 
		BindingGamepad.LeftStickUp => "<Gamepad>/leftStick/up", 
		BindingGamepad.LeftStickDown => "<Gamepad>/leftStick/down", 
		BindingGamepad.LeftStickLeft => "<Gamepad>/leftStick/left", 
		BindingGamepad.LeftStickRight => "<Gamepad>/leftStick/right", 
		BindingGamepad.RightStickUp => "<Gamepad>/rightStick/up", 
		BindingGamepad.RightStickDown => "<Gamepad>/rightStick/down", 
		BindingGamepad.RightStickLeft => "<Gamepad>/rightStick/left", 
		BindingGamepad.RightStickRight => "<Gamepad>/rightStick/right", 
		_ => string.Empty, 
	};

	public override IEnumerable<string> modifierControls
	{
		get
		{
			if (leftStick)
			{
				yield return "<Gamepad>/leftStickPress";
			}
			if (rightStick)
			{
				yield return "<Gamepad>/rightStickPress";
			}
		}
	}

	public SettingsUIGamepadBindingAttribute(string actionName = null)
		: base(actionName, InputManager.DeviceType.Gamepad, ActionType.Button, ActionComponent.Press)
	{
	}

	public SettingsUIGamepadBindingAttribute(AxisComponent component, string actionName = null)
		: base(actionName, InputManager.DeviceType.Gamepad, ActionType.Axis, (ActionComponent)component)
	{
	}

	public SettingsUIGamepadBindingAttribute(Vector2Component component, string actionName = null)
		: base(actionName, InputManager.DeviceType.Gamepad, ActionType.Vector2, (ActionComponent)component)
	{
	}

	public SettingsUIGamepadBindingAttribute(BindingGamepad defaultKey, string actionName = null, bool leftStick = false, bool rightStick = false)
		: this(actionName)
	{
		this.leftStick = leftStick;
		this.rightStick = rightStick;
		this.defaultKey = defaultKey;
	}

	public SettingsUIGamepadBindingAttribute(BindingGamepad defaultKey, AxisComponent component, string actionName = null, bool leftStick = false, bool rightStick = false)
		: this(component, actionName)
	{
		this.leftStick = leftStick;
		this.rightStick = rightStick;
		this.defaultKey = defaultKey;
	}

	public SettingsUIGamepadBindingAttribute(BindingGamepad defaultKey, Vector2Component component, string actionName = null, bool leftStick = false, bool rightStick = false)
		: this(component, actionName)
	{
		this.leftStick = leftStick;
		this.rightStick = rightStick;
		this.defaultKey = defaultKey;
	}

	[Obsolete("Use attribute constructor with BindingGamepad instead of this, it will be removed eventually")]
	public SettingsUIGamepadBindingAttribute(GamepadButton defaultKey, string actionName = null, bool leftStick = false)
		: this((BindingGamepad)(defaultKey + 1), actionName, leftStick)
	{
	}

	[Obsolete("Use attribute constructor with BindingGamepad instead of this, it will be removed eventually")]
	public SettingsUIGamepadBindingAttribute(GamepadButton defaultKey, AxisComponent component, string actionName = null, bool leftStick = false)
		: this((BindingGamepad)(defaultKey + 1), component, actionName, leftStick)
	{
	}

	[Obsolete("Use attribute constructor with BindingGamepad instead of this, it will be removed eventually")]
	public SettingsUIGamepadBindingAttribute(GamepadButton defaultKey, Vector2Component component, string actionName = null, bool leftStick = false)
		: this((BindingGamepad)(defaultKey + 1), component, actionName, leftStick)
	{
	}
}
