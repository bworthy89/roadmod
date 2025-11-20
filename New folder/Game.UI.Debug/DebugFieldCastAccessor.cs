using System;
using Colossal.Annotations;
using Game.Reflection;
using UnityEngine.Rendering;

namespace Game.UI.Debug;

public class DebugFieldCastAccessor<T, U> : ITypedValueAccessor<T>, IValueAccessor
{
	private DebugUI.Field<U> m_Field;

	private readonly Converter<U, T> m_FromField;

	private readonly Converter<T, U> m_ToField;

	public Type valueType => typeof(T);

	public IValueAccessor parent => null;

	public DebugFieldCastAccessor(DebugUI.Field<U> field, Converter<U, T> fromField, [NotNull] Converter<T, U> toField)
	{
		m_Field = field;
		m_FromField = fromField;
		m_ToField = toField;
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
		return m_FromField(m_Field.GetValue());
	}

	public void SetTypedValue(T value)
	{
		m_Field.SetValue(m_ToField(value));
	}
}
