using Game.Settings;
using UnityEngine.InputSystem;

namespace Game.Input;

public class CameraZoomProcessor : PlatformProcessor<float>
{
	public float m_Scale = 1f;

	public override float Process(float value, InputControl control)
	{
		if (!base.needProcess)
		{
			return value;
		}
		Game.Settings.InputSettings input = SharedSettings.instance.input;
		value *= m_Scale;
		float num = value;
		value = num * m_DeviceType switch
		{
			ProcessorDeviceType.Mouse => input.mouseZoomSensitivity, 
			ProcessorDeviceType.Keyboard => input.keyboardZoomSensitivity, 
			ProcessorDeviceType.Gamepad => input.gamepadZoomSensitivity, 
			_ => 1f, 
		};
		return value;
	}
}
