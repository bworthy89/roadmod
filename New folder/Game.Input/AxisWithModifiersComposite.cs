using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace Game.Input;

[DisplayStringFormat("{binding}")]
[DisplayName("CO Axis 1D With Modifiers")]
public class AxisWithModifiersComposite : AnalogValueInputBindingComposite<float>
{
	[InputControl(layout = "Axis")]
	public int binding;

	[InputControl(layout = "Button")]
	public int modifier;

	public override float ReadValue(ref InputBindingCompositeContext context)
	{
		if (m_IsDummy)
		{
			return 0f;
		}
		if (m_Mode == Mode.Analog)
		{
			return CompositeUtility.ReadValue(ref context, binding, base.allowModifiers, modifier, DefaultComparer<float>.instance);
		}
		if (!CompositeUtility.ReadValueAsButton(ref context, binding, base.allowModifiers, modifier))
		{
			return 0f;
		}
		return 1f;
	}

	public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
	{
		return Mathf.Abs(ReadValue(ref context));
	}

	public static InputManager.CompositeData GetCompositeData()
	{
		return new InputManager.CompositeData(CompositeUtility.GetCompositeTypeName(typeof(AxisWithModifiersComposite)), ActionType.Button, new InputManager.CompositeComponentData[1]
		{
			new InputManager.CompositeComponentData(ActionComponent.Press, "binding", "modifier")
		});
	}
}
