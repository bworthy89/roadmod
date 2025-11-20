using System;
using System.Collections.Generic;
using UnityEngine.InputSystem;

namespace Game.Input;

public static class CompositeUtility
{
	public unsafe static T ReadValue<T>(ref InputBindingCompositeContext context, int button, bool allowModifiers, int modifier, IComparer<T> comparer) where T : struct
	{
		int controlIndex;
		if (context.m_State != null && CheckModifiers(ref context, allowModifiers, modifier))
		{
			return context.m_State.ReadCompositePartValue<T, IComparer<T>>(context.m_BindingIndex, button, null, out controlIndex, comparer);
		}
		return default(T);
	}

	public static bool ReadValueAsButton(ref InputBindingCompositeContext context, int button, bool allowModifiers, int modifier)
	{
		if (context.m_State != null && CheckModifiers(ref context, allowModifiers, modifier))
		{
			return context.ReadValueAsButton(button);
		}
		return false;
	}

	public static bool CheckModifiers(ref InputBindingCompositeContext context, bool allowModifiers, int modifier)
	{
		float num = ((allowModifiers && modifier != 0) ? context.ReadValue<float, ModifiersComparer>(modifier) : 1f);
		if (!float.IsNaN(num))
		{
			return num != 0f;
		}
		return false;
	}

	public static ActionType GetActionType(this ActionComponent component)
	{
		return component switch
		{
			ActionComponent.Press => ActionType.Button, 
			ActionComponent.Negative => ActionType.Axis, 
			ActionComponent.Positive => ActionType.Axis, 
			ActionComponent.Down => ActionType.Vector2, 
			ActionComponent.Up => ActionType.Vector2, 
			ActionComponent.Left => ActionType.Vector2, 
			ActionComponent.Right => ActionType.Vector2, 
			_ => throw new ArgumentOutOfRangeException("component", component, null), 
		};
	}

	public static string GetCompositeTypeName(this ActionType actionType)
	{
		return actionType switch
		{
			ActionType.Button => GetCompositeTypeName(typeof(ButtonWithModifiersComposite)), 
			ActionType.Axis => GetCompositeTypeName(typeof(AxisSeparatedWithModifiersComposite)), 
			ActionType.Vector2 => GetCompositeTypeName(typeof(Vector2SeparatedWithModifiersComposite)), 
			_ => throw new ArgumentOutOfRangeException("actionType", actionType, null), 
		};
	}

	public static InputActionType GetInputActionType(this ActionType actionType)
	{
		return actionType switch
		{
			ActionType.Button => InputActionType.Button, 
			ActionType.Axis => InputActionType.Value, 
			ActionType.Vector2 => InputActionType.Value, 
			_ => throw new ArgumentOutOfRangeException("actionType", actionType, null), 
		};
	}

	public static string GetExpectedControlLayout(this ActionType actionType)
	{
		return actionType switch
		{
			ActionType.Button => "Button", 
			ActionType.Axis => "Axis", 
			ActionType.Vector2 => "Vector2", 
			_ => throw new ArgumentOutOfRangeException("actionType", actionType, null), 
		};
	}

	public static Guid GetGuid(long part1, long part2)
	{
		byte[] array = new byte[16];
		Array.Copy(BitConverter.GetBytes(part1), 0, array, 0, 8);
		Array.Copy(BitConverter.GetBytes(part2), 0, array, 8, 8);
		return new Guid(array);
	}

	public static void SetGuid(Guid guid, out long part1, out long part2)
	{
		byte[] value = guid.ToByteArray();
		part1 = BitConverter.ToInt64(value, 0);
		part2 = BitConverter.ToInt64(value, 8);
	}

	public static string GetCompositeTypeName(Type type)
	{
		string text = type.Name;
		if (text.EndsWith("Composite"))
		{
			text = text.Substring(0, text.Length - "Composite".Length);
		}
		return text;
	}
}
