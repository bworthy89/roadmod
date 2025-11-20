using System.Collections.Generic;
using Game.Input;

namespace Game.Settings;

public class SettingsUIMouseBindingAttribute : SettingsUIKeybindingAttribute
{
	public readonly BindingMouse defaultKey;

	public readonly bool alt;

	public readonly bool ctrl;

	public readonly bool shift;

	public override string control => defaultKey switch
	{
		BindingMouse.None => string.Empty, 
		BindingMouse.Left => "<Mouse>/leftButton", 
		BindingMouse.Middle => "<Mouse>/middleButton", 
		BindingMouse.Right => "<Mouse>/rightButton", 
		BindingMouse.Forward => "<Mouse>/forwardButton", 
		BindingMouse.Backward => "<Mouse>/backButton", 
		_ => string.Empty, 
	};

	public override IEnumerable<string> modifierControls
	{
		get
		{
			if (shift)
			{
				yield return "<Keyboard>/shift";
			}
			if (ctrl)
			{
				yield return "<Keyboard>/ctrl";
			}
			if (alt)
			{
				yield return "<Keyboard>/alt";
			}
		}
	}

	public SettingsUIMouseBindingAttribute(string actionName = null)
		: base(actionName, InputManager.DeviceType.Mouse, ActionType.Button, ActionComponent.Press)
	{
	}

	public SettingsUIMouseBindingAttribute(AxisComponent component, string actionName = null)
		: base(actionName, InputManager.DeviceType.Mouse, ActionType.Axis, (ActionComponent)component)
	{
	}

	public SettingsUIMouseBindingAttribute(Vector2Component component, string actionName = null)
		: base(actionName, InputManager.DeviceType.Mouse, ActionType.Vector2, (ActionComponent)component)
	{
	}

	public SettingsUIMouseBindingAttribute(BindingMouse defaultKey, string actionName = null, bool alt = false, bool ctrl = false, bool shift = false)
		: this(actionName)
	{
		this.alt = alt;
		this.ctrl = ctrl;
		this.shift = shift;
		this.defaultKey = defaultKey;
	}

	public SettingsUIMouseBindingAttribute(BindingMouse defaultKey, AxisComponent component, string actionName = null, bool alt = false, bool ctrl = false, bool shift = false)
		: this(component, actionName)
	{
		this.alt = alt;
		this.ctrl = ctrl;
		this.shift = shift;
		this.defaultKey = defaultKey;
	}

	public SettingsUIMouseBindingAttribute(BindingMouse defaultKey, Vector2Component component, string actionName = null, bool alt = false, bool ctrl = false, bool shift = false)
		: this(component, actionName)
	{
		this.alt = alt;
		this.ctrl = ctrl;
		this.shift = shift;
		this.defaultKey = defaultKey;
	}
}
