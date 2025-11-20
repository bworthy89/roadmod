using Game.Settings;
using UnityEngine.InputSystem;

namespace Game.Input;

public class ScrollSensitivityProcessor : InputProcessor<float>
{
	public override float Process(float value, InputControl control)
	{
		return value * SharedSettings.instance.input.finalScrollSensitivity;
	}
}
