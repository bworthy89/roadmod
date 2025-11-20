using Game.Settings;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input;

public class CameraRotateProcessor : PlatformProcessor<Vector2>
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
		ref float x = ref value.x;
		float num = x;
		x = num * m_DeviceType switch
		{
			ProcessorDeviceType.Mouse => input.mouseInvertX ? (0f - input.mouseRotateSensitivity) : input.mouseRotateSensitivity, 
			ProcessorDeviceType.Keyboard => input.keyboardRotateSensitivity, 
			ProcessorDeviceType.Gamepad => input.gamepadInvertX ? (0f - input.gamepadRotateSensitivity) : input.gamepadRotateSensitivity, 
			_ => 1f, 
		};
		x = ref value.y;
		float num2 = x;
		x = num2 * m_DeviceType switch
		{
			ProcessorDeviceType.Mouse => input.mouseInvertY ? (0f - input.mouseRotateSensitivity) : input.mouseRotateSensitivity, 
			ProcessorDeviceType.Keyboard => input.keyboardRotateSensitivity, 
			ProcessorDeviceType.Gamepad => (!input.gamepadInvertY) ? 1 : (-1), 
			_ => 1f, 
		};
		return value;
	}
}
