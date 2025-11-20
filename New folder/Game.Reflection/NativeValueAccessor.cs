using System;
using Colossal.Annotations;
using Colossal.Collections;

namespace Game.Reflection;

public class NativeValueAccessor<T> : ITypedValueAccessor<T>, IValueAccessor, IEquatable<NativeValueAccessor<T>> where T : unmanaged
{
	[NotNull]
	private readonly IValueAccessor m_Parent;

	public Type valueType => typeof(T);

	public IValueAccessor parent => m_Parent;

	public NativeValueAccessor([NotNull] IValueAccessor parent)
	{
		m_Parent = parent ?? throw new ArgumentNullException("parent");
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
		return ((NativeValue<T>)m_Parent.GetValue()).value;
	}

	public void SetTypedValue(T value)
	{
		NativeValue<T> nativeValue = (NativeValue<T>)m_Parent.GetValue();
		nativeValue.value = value;
	}

	public bool Equals(NativeValueAccessor<T> other)
	{
		if (other == null)
		{
			return false;
		}
		if (this == other)
		{
			return true;
		}
		return m_Parent.Equals(other.m_Parent);
	}

	public override bool Equals(object obj)
	{
		if (obj == null)
		{
			return false;
		}
		if (this == obj)
		{
			return true;
		}
		if (obj.GetType() != GetType())
		{
			return false;
		}
		return Equals((NativeValueAccessor<T>)obj);
	}

	public override int GetHashCode()
	{
		return m_Parent.GetHashCode();
	}
}
