using System;
using Game.Reflection;
using UnityEngine.Rendering;

namespace Game.UI.Debug;

public class DebugFieldAccessor<T> : ITypedValueAccessor<T>, IValueAccessor
{
	private DebugUI.Field<T> m_Field;

	public Type valueType => typeof(T);

	public IValueAccessor parent => null;

	public DebugFieldAccessor(DebugUI.Field<T> field)
	{
		m_Field = field;
	}

	public object GetValue()
	{
		return GetTypedValue();
	}

	public void SetValue(object value)
	{
		SetTypedValue((T)value);
	}

	public T GetTypedValue()
	{
		return m_Field.GetValue();
	}

	public void SetTypedValue(T value)
	{
		m_Field.SetValue(value);
	}
}
