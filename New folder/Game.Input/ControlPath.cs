using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Colossal.UI.Binding;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

namespace Game.Input;

[DebuggerDisplay("{name} ({displayName})")]
public struct ControlPath : IJsonWritable
{
	private static Dictionary<string, bool> m_IsLatinLayout = new Dictionary<string, bool>();

	public string name;

	public InputManager.DeviceType device;

	public string displayName;

	public static ControlPath Get(string path)
	{
		if (string.IsNullOrEmpty(path))
		{
			return new ControlPath
			{
				name = string.Empty,
				device = InputManager.DeviceType.None,
				displayName = string.Empty
			};
		}
		InputControlPath.ParsedPathComponent[] source = InputControlPath.Parse(path).ToArray();
		string text = string.Join("/", from p in source
			where string.IsNullOrEmpty(p.layout)
			select p.name);
		string layout = source.FirstOrDefault((InputControlPath.ParsedPathComponent p) => !string.IsNullOrEmpty(p.layout)).layout;
		return new ControlPath
		{
			name = text,
			device = layout.ToDeviceType(),
			displayName = ((text.Length == 1 && char.IsLetter(text[0])) ? text.ToUpper() : text)
		};
	}

	public static bool IsLatinLikeLayout(Keyboard keyboard)
	{
		if (!m_IsLatinLayout.TryGetValue(keyboard.keyboardLayout, out var value))
		{
			value = Enumerable.Range(15, 26).All((int k) => IsLatinOrPunctuation(keyboard[(Key)k].displayName));
			m_IsLatinLayout[keyboard.keyboardLayout] = value;
		}
		return value;
	}

	public static bool NeedLocalName(Keyboard keyboard, KeyControl control)
	{
		switch (control.keyCode)
		{
		case Key.Space:
		case Key.Enter:
		case Key.Tab:
		case Key.Digit1:
		case Key.Digit2:
		case Key.Digit3:
		case Key.Digit4:
		case Key.Digit5:
		case Key.Digit6:
		case Key.Digit7:
		case Key.Digit8:
		case Key.Digit9:
		case Key.Digit0:
		case Key.LeftShift:
		case Key.RightShift:
		case Key.LeftAlt:
		case Key.RightAlt:
		case Key.LeftCtrl:
		case Key.RightCtrl:
		case Key.LeftMeta:
		case Key.RightMeta:
		case Key.Escape:
		case Key.LeftArrow:
		case Key.RightArrow:
		case Key.UpArrow:
		case Key.DownArrow:
		case Key.Backspace:
		case Key.PageDown:
		case Key.PageUp:
		case Key.Home:
		case Key.End:
		case Key.Delete:
		case Key.Numpad0:
		case Key.Numpad1:
		case Key.Numpad2:
		case Key.Numpad3:
		case Key.Numpad4:
		case Key.Numpad5:
		case Key.Numpad6:
		case Key.Numpad7:
		case Key.Numpad8:
		case Key.Numpad9:
			return false;
		case Key.OEM1:
		case Key.OEM2:
		case Key.OEM3:
		case Key.OEM4:
		case Key.OEM5:
			return true;
		default:
			return IsLatinLikeLayout(keyboard);
		}
	}

	private static bool IsLatinOrPunctuation(string displayName)
	{
		if (!string.IsNullOrEmpty(displayName))
		{
			if (!IsLatinLater(displayName))
			{
				return char.IsPunctuation(displayName[0]);
			}
			return true;
		}
		return false;
	}

	private static bool IsLatinLater(string displayName)
	{
		if (!string.IsNullOrEmpty(displayName) && char.IsLetterOrDigit(displayName[0]))
		{
			return displayName[0] <= 'ÿ';
		}
		return false;
	}

	public void Write(IJsonWriter writer)
	{
		writer.TypeBegin(typeof(ControlPath).FullName);
		writer.PropertyName("name");
		writer.Write(name);
		writer.PropertyName("device");
		writer.Write(device.ToString());
		writer.PropertyName("displayName");
		writer.Write(displayName);
		writer.TypeEnd();
	}

	public static string ToHumanReadablePath(string path, InputControlPath.HumanReadableStringOptions options = InputControlPath.HumanReadableStringOptions.OmitDevice)
	{
		return InputControlPath.ToHumanReadableString(path, options);
	}
}
