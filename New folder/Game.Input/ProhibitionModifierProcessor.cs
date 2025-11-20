using UnityEngine.InputSystem;

namespace Game.Input;

public class ProhibitionModifierProcessor : InputProcessor<float>
{
	public override float Process(float value, InputControl control)
	{
		value = ((value != 0f) ? float.NaN : 1f);
		return value;
	}
}
