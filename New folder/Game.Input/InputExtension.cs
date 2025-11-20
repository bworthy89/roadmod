using System;
using UnityEngine.InputSystem;

namespace Game.Input;

public static class InputExtension
{
	public static InputManager.DeviceType ToDeviceType(this InputBinding? mask)
	{
		if (!mask.HasValue)
		{
			return InputManager.DeviceType.All;
		}
		InputManager.DeviceType deviceType = InputManager.DeviceType.None;
		if (mask.Value.groups.Contains("Keyboard"))
		{
			deviceType |= InputManager.DeviceType.Keyboard;
		}
		if (mask.Value.groups.Contains("Mouse"))
		{
			deviceType |= InputManager.DeviceType.Mouse;
		}
		if (mask.Value.groups.Contains("Gamepad"))
		{
			deviceType |= InputManager.DeviceType.Gamepad;
		}
		return deviceType;
	}

	public static InputManager.DeviceType ToDeviceType(this string group)
	{
		return group switch
		{
			"Keyboard" => InputManager.DeviceType.Keyboard, 
			"Mouse" => InputManager.DeviceType.Mouse, 
			"Gamepad" => InputManager.DeviceType.Gamepad, 
			"DualShockGamepad" => InputManager.DeviceType.Gamepad, 
			"XInputController" => InputManager.DeviceType.Gamepad, 
			_ => throw new ArgumentException("Unsupported device type \"" + group + "\""), 
		};
	}

	public static InputManager.DeviceType ToDeviceType(this InputManager.ControlScheme scheme)
	{
		return scheme switch
		{
			InputManager.ControlScheme.KeyboardAndMouse => InputManager.DeviceType.Keyboard | InputManager.DeviceType.Mouse, 
			InputManager.ControlScheme.Gamepad => InputManager.DeviceType.Gamepad, 
			_ => InputManager.DeviceType.None, 
		};
	}

	public static InputBinding? ToInputBinding(this InputManager.DeviceType type)
	{
		return type switch
		{
			InputManager.DeviceType.All => null, 
			InputManager.DeviceType.None => InputBinding.MaskByGroup(string.Empty), 
			_ => InputBinding.MaskByGroups(((type & InputManager.DeviceType.Keyboard) != InputManager.DeviceType.None) ? "Keyboard" : string.Empty, ((type & InputManager.DeviceType.Mouse) != InputManager.DeviceType.None) ? "Mouse" : string.Empty, ((type & InputManager.DeviceType.Gamepad) != InputManager.DeviceType.None) ? "Gamepad" : string.Empty), 
		};
	}

	public static UIBaseInputAction.Transform ToTransform(this ActionComponent component)
	{
		return component switch
		{
			ActionComponent.Press => UIBaseInputAction.Transform.Press, 
			ActionComponent.Negative => UIBaseInputAction.Transform.Negative, 
			ActionComponent.Positive => UIBaseInputAction.Transform.Positive, 
			ActionComponent.Down => UIBaseInputAction.Transform.Down, 
			ActionComponent.Up => UIBaseInputAction.Transform.Up, 
			ActionComponent.Left => UIBaseInputAction.Transform.Left, 
			ActionComponent.Right => UIBaseInputAction.Transform.Right, 
			_ => UIBaseInputAction.Transform.Press, 
		};
	}

	public static string GetPath(this InputBinding binding, InputManager.PathType pathType)
	{
		return pathType switch
		{
			InputManager.PathType.Effective => binding.effectivePath, 
			InputManager.PathType.Original => binding.path, 
			InputManager.PathType.Overridden => binding.overridePath, 
			_ => binding.effectivePath, 
		};
	}

	public static string GetProcessors(this InputBinding binding, InputManager.PathType pathType)
	{
		return pathType switch
		{
			InputManager.PathType.Effective => binding.effectiveProcessors, 
			InputManager.PathType.Original => binding.processors, 
			InputManager.PathType.Overridden => binding.overrideProcessors, 
			_ => binding.effectivePath, 
		};
	}

	public static string GetInteractions(this InputBinding binding, InputManager.PathType pathType)
	{
		return pathType switch
		{
			InputManager.PathType.Effective => binding.effectiveInteractions, 
			InputManager.PathType.Original => binding.interactions, 
			InputManager.PathType.Overridden => binding.overrideInteractions, 
			_ => binding.effectivePath, 
		};
	}
}
