using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Utilities;

namespace Game.Input;

[DisplayStringFormat("{binding}")]
[DisplayName("CO Camera Vector 2D With Modifiers")]
public class CameraVector2WithModifiersComposite : AnalogValueInputBindingComposite<Vector2>
{
	public bool m_ModifierActuatesControl;

	[InputControl(layout = "Vector2")]
	public int vector;

	[InputControl(layout = "Button")]
	public int trigger;

	public override Vector2 ReadValue(ref InputBindingCompositeContext context)
	{
		if (m_IsDummy)
		{
			return default(Vector2);
		}
		if (m_Mode == Mode.Analog)
		{
			return CompositeUtility.ReadValue(ref context, vector, allowModifiers: true, trigger, Vector2Comparer.instance);
		}
		if (!CompositeUtility.ReadValueAsButton(ref context, vector, allowModifiers: true, trigger))
		{
			return Vector2.zero;
		}
		return Vector2.one;
	}

	public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
	{
		if (CompositeUtility.CheckModifiers(ref context, allowModifiers: true, trigger))
		{
			if (m_ModifierActuatesControl && trigger != 0)
			{
				return Mathf.Abs(context.ReadValue<float, ModifiersComparer>(trigger));
			}
			return context.EvaluateMagnitude(vector);
		}
		return 0f;
	}

	public static InputManager.CompositeData GetCompositeData()
	{
		return new InputManager.CompositeData(CompositeUtility.GetCompositeTypeName(typeof(CameraVector2WithModifiersComposite)), ActionType.Button, new InputManager.CompositeComponentData[1]
		{
			new InputManager.CompositeComponentData(ActionComponent.Press, "trigger", string.Empty)
		});
	}
}
