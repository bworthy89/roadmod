using System;
using System.Collections.Generic;
using Game.Input;
using UnityEngine.InputSystem;

namespace Game.Settings;

[AttributeUsage(AttributeTargets.Property)]
public class SettingsUIKeyboardBindingAttribute : SettingsUIKeybindingAttribute
{
	public readonly BindingKeyboard defaultKey;

	public readonly bool alt;

	public readonly bool ctrl;

	public readonly bool shift;

	public override string control => defaultKey switch
	{
		BindingKeyboard.None => string.Empty, 
		BindingKeyboard.Space => "<Keyboard>/space", 
		BindingKeyboard.Enter => "<Keyboard>/enter", 
		BindingKeyboard.Tab => "<Keyboard>/tab", 
		BindingKeyboard.Backquote => "<Keyboard>/backquote", 
		BindingKeyboard.Quote => "<Keyboard>/quote", 
		BindingKeyboard.Semicolon => "<Keyboard>/semicolon", 
		BindingKeyboard.Comma => "<Keyboard>/comma", 
		BindingKeyboard.Period => "<Keyboard>/period", 
		BindingKeyboard.Slash => "<Keyboard>/slash", 
		BindingKeyboard.Backslash => "<Keyboard>/backslash", 
		BindingKeyboard.LeftBracket => "<Keyboard>/leftBracket", 
		BindingKeyboard.RightBracket => "<Keyboard>/rightBracket", 
		BindingKeyboard.Minus => "<Keyboard>/minus", 
		BindingKeyboard.Equals => "<Keyboard>/equals", 
		BindingKeyboard.A => "<Keyboard>/a", 
		BindingKeyboard.B => "<Keyboard>/b", 
		BindingKeyboard.C => "<Keyboard>/c", 
		BindingKeyboard.D => "<Keyboard>/d", 
		BindingKeyboard.E => "<Keyboard>/e", 
		BindingKeyboard.F => "<Keyboard>/f", 
		BindingKeyboard.G => "<Keyboard>/g", 
		BindingKeyboard.H => "<Keyboard>/h", 
		BindingKeyboard.I => "<Keyboard>/i", 
		BindingKeyboard.J => "<Keyboard>/j", 
		BindingKeyboard.K => "<Keyboard>/k", 
		BindingKeyboard.L => "<Keyboard>/l", 
		BindingKeyboard.M => "<Keyboard>/m", 
		BindingKeyboard.N => "<Keyboard>/n", 
		BindingKeyboard.O => "<Keyboard>/o", 
		BindingKeyboard.P => "<Keyboard>/p", 
		BindingKeyboard.Q => "<Keyboard>/q", 
		BindingKeyboard.R => "<Keyboard>/r", 
		BindingKeyboard.S => "<Keyboard>/s", 
		BindingKeyboard.T => "<Keyboard>/t", 
		BindingKeyboard.U => "<Keyboard>/u", 
		BindingKeyboard.V => "<Keyboard>/v", 
		BindingKeyboard.W => "<Keyboard>/w", 
		BindingKeyboard.X => "<Keyboard>/x", 
		BindingKeyboard.Y => "<Keyboard>/y", 
		BindingKeyboard.Z => "<Keyboard>/z", 
		BindingKeyboard.Digit1 => "<Keyboard>/1", 
		BindingKeyboard.Digit2 => "<Keyboard>/2", 
		BindingKeyboard.Digit3 => "<Keyboard>/3", 
		BindingKeyboard.Digit4 => "<Keyboard>/4", 
		BindingKeyboard.Digit5 => "<Keyboard>/5", 
		BindingKeyboard.Digit6 => "<Keyboard>/6", 
		BindingKeyboard.Digit7 => "<Keyboard>/7", 
		BindingKeyboard.Digit8 => "<Keyboard>/8", 
		BindingKeyboard.Digit9 => "<Keyboard>/9", 
		BindingKeyboard.Digit0 => "<Keyboard>/0", 
		BindingKeyboard.Escape => "<Keyboard>/escape", 
		BindingKeyboard.LeftArrow => "<Keyboard>/leftArrow", 
		BindingKeyboard.RightArrow => "<Keyboard>/rightArrow", 
		BindingKeyboard.UpArrow => "<Keyboard>/upArrow", 
		BindingKeyboard.DownArrow => "<Keyboard>/downArrow", 
		BindingKeyboard.Backspace => "<Keyboard>/backspace", 
		BindingKeyboard.PageDown => "<Keyboard>/pageDown", 
		BindingKeyboard.PageUp => "<Keyboard>/pageUp", 
		BindingKeyboard.Home => "<Keyboard>/home", 
		BindingKeyboard.End => "<Keyboard>/end", 
		BindingKeyboard.Delete => "<Keyboard>/delete", 
		BindingKeyboard.NumpadEnter => "<Keyboard>/numpadEnter", 
		BindingKeyboard.NumpadDivide => "<Keyboard>/numpadDivide", 
		BindingKeyboard.NumpadMultiply => "<Keyboard>/numpadMultiply", 
		BindingKeyboard.NumpadPlus => "<Keyboard>/numpadPlus", 
		BindingKeyboard.NumpadMinus => "<Keyboard>/numpadMinus", 
		BindingKeyboard.NumpadPeriod => "<Keyboard>/numpadPeriod", 
		BindingKeyboard.NumpadEquals => "<Keyboard>/numpadEquals", 
		BindingKeyboard.Numpad0 => "<Keyboard>/numpad0", 
		BindingKeyboard.Numpad1 => "<Keyboard>/numpad1", 
		BindingKeyboard.Numpad2 => "<Keyboard>/numpad2", 
		BindingKeyboard.Numpad3 => "<Keyboard>/numpad3", 
		BindingKeyboard.Numpad4 => "<Keyboard>/numpad4", 
		BindingKeyboard.Numpad5 => "<Keyboard>/numpad5", 
		BindingKeyboard.Numpad6 => "<Keyboard>/numpad6", 
		BindingKeyboard.Numpad7 => "<Keyboard>/numpad7", 
		BindingKeyboard.Numpad8 => "<Keyboard>/numpad8", 
		BindingKeyboard.Numpad9 => "<Keyboard>/numpad9", 
		BindingKeyboard.F1 => "<Keyboard>/f1", 
		BindingKeyboard.F2 => "<Keyboard>/f2", 
		BindingKeyboard.F3 => "<Keyboard>/f3", 
		BindingKeyboard.F4 => "<Keyboard>/f4", 
		BindingKeyboard.F5 => "<Keyboard>/f5", 
		BindingKeyboard.F6 => "<Keyboard>/f6", 
		BindingKeyboard.F7 => "<Keyboard>/f7", 
		BindingKeyboard.F8 => "<Keyboard>/f8", 
		BindingKeyboard.F9 => "<Keyboard>/f9", 
		BindingKeyboard.F10 => "<Keyboard>/f10", 
		BindingKeyboard.F11 => "<Keyboard>/f11", 
		BindingKeyboard.F12 => "<Keyboard>/f12", 
		BindingKeyboard.OEM1 => "<Keyboard>/OEM1", 
		BindingKeyboard.OEM2 => "<Keyboard>/OEM2", 
		BindingKeyboard.OEM3 => "<Keyboard>/OEM3", 
		BindingKeyboard.OEM4 => "<Keyboard>/OEM4", 
		BindingKeyboard.OEM5 => "<Keyboard>/OEM5", 
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

	public SettingsUIKeyboardBindingAttribute(string actionName = null)
		: base(actionName, Game.Input.InputManager.DeviceType.Keyboard, ActionType.Button, ActionComponent.Press)
	{
	}

	public SettingsUIKeyboardBindingAttribute(AxisComponent component, string actionName = null)
		: base(actionName, Game.Input.InputManager.DeviceType.Keyboard, ActionType.Axis, (ActionComponent)component)
	{
	}

	public SettingsUIKeyboardBindingAttribute(Vector2Component component, string actionName = null)
		: base(actionName, Game.Input.InputManager.DeviceType.Keyboard, ActionType.Vector2, (ActionComponent)component)
	{
	}

	public SettingsUIKeyboardBindingAttribute(BindingKeyboard defaultKey, string actionName = null, bool alt = false, bool ctrl = false, bool shift = false)
		: this(actionName)
	{
		this.alt = alt;
		this.ctrl = ctrl;
		this.shift = shift;
		this.defaultKey = defaultKey;
	}

	public SettingsUIKeyboardBindingAttribute(BindingKeyboard defaultKey, AxisComponent component, string actionName = null, bool alt = false, bool ctrl = false, bool shift = false)
		: this(component, actionName)
	{
		this.alt = alt;
		this.ctrl = ctrl;
		this.shift = shift;
		this.defaultKey = defaultKey;
	}

	public SettingsUIKeyboardBindingAttribute(BindingKeyboard defaultKey, Vector2Component component, string actionName = null, bool alt = false, bool ctrl = false, bool shift = false)
		: this(component, actionName)
	{
		this.alt = alt;
		this.ctrl = ctrl;
		this.shift = shift;
		this.defaultKey = defaultKey;
	}

	[Obsolete("Use attribute constructor with BindingKeyboard instead of this, it will be removed eventually")]
	public SettingsUIKeyboardBindingAttribute(Key defaultKey, string actionName = null, bool alt = false, bool ctrl = false, bool shift = false)
		: this((BindingKeyboard)defaultKey, actionName, alt, ctrl, shift)
	{
	}

	[Obsolete("Use attribute constructor with BindingKeyboard instead of this, it will be removed eventually")]
	public SettingsUIKeyboardBindingAttribute(Key defaultKey, AxisComponent component, string actionName = null, bool alt = false, bool ctrl = false, bool shift = false)
		: this((BindingKeyboard)defaultKey, component, actionName, alt, ctrl, shift)
	{
	}

	[Obsolete("Use attribute constructor with BindingKeyboard instead of this, it will be removed eventually")]
	public SettingsUIKeyboardBindingAttribute(Key defaultKey, Vector2Component component, string actionName = null, bool alt = false, bool ctrl = false, bool shift = false)
		: this((BindingKeyboard)defaultKey, component, actionName, alt, ctrl, shift)
	{
	}
}
