using System.ComponentModel;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Layouts;
using UnityEngine.InputSystem.Processors;
using UnityEngine.InputSystem.Utilities;

namespace Game.Input;

[DisplayStringFormat("{negative}/{positive}")]
[DisplayName("CO Positive/Negative Binding With Modifiers")]
public class AxisSeparatedWithModifiersComposite : AnalogValueInputBindingComposite<float>
{
	public enum WhichSideWins
	{
		Neither,
		Positive,
		Negative
	}

	[InputControl(layout = "Button")]
	public int negative;

	[InputControl(layout = "Button")]
	public int positive;

	[InputControl(layout = "Button")]
	public int negativeModifier;

	[InputControl(layout = "Button")]
	public int positiveModifier;

	public float m_MinValue = -1f;

	public float m_MaxValue = 1f;

	public WhichSideWins m_WhichSideWins;

	public float midPoint => (m_MaxValue + m_MinValue) / 2f;

	public override float ReadValue(ref InputBindingCompositeContext context)
	{
		if (m_IsDummy)
		{
			return 0f;
		}
		float num;
		float num2;
		if (m_Mode == Mode.Analog)
		{
			num = Mathf.Abs(CompositeUtility.ReadValue(ref context, negative, base.allowModifiers, negativeModifier, DefaultComparer<float>.instance));
			num2 = Mathf.Abs(CompositeUtility.ReadValue(ref context, positive, base.allowModifiers, positiveModifier, DefaultComparer<float>.instance));
		}
		else
		{
			num = (CompositeUtility.ReadValueAsButton(ref context, negative, base.allowModifiers, negativeModifier) ? 1f : 0f);
			num2 = (CompositeUtility.ReadValueAsButton(ref context, positive, base.allowModifiers, positiveModifier) ? 1f : 0f);
		}
		bool flag = num > Mathf.Epsilon;
		bool flag2 = num2 > Mathf.Epsilon;
		if (flag == flag2)
		{
			switch (m_WhichSideWins)
			{
			case WhichSideWins.Negative:
				flag2 = false;
				break;
			case WhichSideWins.Positive:
				flag = false;
				break;
			case WhichSideWins.Neither:
				return midPoint;
			}
		}
		float num3 = midPoint;
		if (flag)
		{
			return num3 - (num3 - m_MinValue) * num;
		}
		if (flag2)
		{
			return num3 + (m_MaxValue - num3) * num2;
		}
		return midPoint;
	}

	public override float EvaluateMagnitude(ref InputBindingCompositeContext context)
	{
		float num = ReadValue(ref context);
		if (num < midPoint)
		{
			num = Mathf.Abs(num - midPoint);
			return NormalizeProcessor.Normalize(num, 0f, Mathf.Abs(m_MinValue), 0f);
		}
		num = Mathf.Abs(num - midPoint);
		return NormalizeProcessor.Normalize(num, 0f, Mathf.Abs(m_MaxValue), 0f);
	}

	public static InputManager.CompositeData GetCompositeData()
	{
		return new InputManager.CompositeData(CompositeUtility.GetCompositeTypeName(typeof(AxisSeparatedWithModifiersComposite)), ActionType.Axis, new InputManager.CompositeComponentData[2]
		{
			new InputManager.CompositeComponentData(ActionComponent.Negative, "negative", "negativeModifier"),
			new InputManager.CompositeComponentData(ActionComponent.Positive, "positive", "positiveModifier")
		});
	}
}
