using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace Game.Input;

[DisplayStringFormat("{binding}")]
[DisplayName("CO Vector 2D With Modifiers")]
public class Vector2WithModifiersComposite : AnalogValueInputBindingComposite<Vector2>
{
	[InputControl(layout = "Vector2")]
	public int binding;

	[InputControl(layout = "Button")]
	public int modifier;

	public override Vector2 ReadValue(ref InputBindingCompositeContext context)
	{
		if (m_IsDummy)
		{
			return default(Vector2);
		}
		if (m_Mode == Mode.Analog)
		{
			return CompositeUtility.ReadValue(ref context, binding, base.allowModifiers, modifier, Vector2Comparer.instance);
		}
		if (!CompositeUtility.ReadValueAsButton(ref context, binding, base.allowModifiers, modifier))
		{
			return Vector2.zero;
		}
		return Vector2.one;
	}

	public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
	{
		return ReadValue(ref context).magnitude;
	}

	public static InputManager.CompositeData GetCompositeData()
	{
		return new InputManager.CompositeData(CompositeUtility.GetCompositeTypeName(typeof(Vector2WithModifiersComposite)), ActionType.Button, new InputManager.CompositeComponentData[1]
		{
			new InputManager.CompositeComponentData(ActionComponent.Press, "binding", "modifier")
		});
	}
}
