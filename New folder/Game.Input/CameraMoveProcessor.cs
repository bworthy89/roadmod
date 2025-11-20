using Game.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input;

public class CameraMoveProcessor : PlatformProcessor<Vector2>
{
	public float m_ScaleX = 1f;

	public float m_ScaleY = 1f;

	public override Vector2 Process(Vector2 value, InputControl control)
	{
		if (!base.needProcess)
		{
			return value;
		}
		Game.Settings.InputSettings input = SharedSettings.instance.input;
		value.x *= m_ScaleX;
		value.y *= m_ScaleY;
		Vector2 vector = value;
		value = vector * m_DeviceType switch
		{
			ProcessorDeviceType.Mouse => input.mouseMoveSensitivity, 
			ProcessorDeviceType.Keyboard => input.keyboardMoveSensitivity, 
			ProcessorDeviceType.Gamepad => input.gamepadMoveSensitivity, 
			_ => 1f, 
		};
		return value;
	}
}
