using System;

namespace Game.Reflection;

public class ObjectAccessor<T> : ITypedValueAccessor<T>, IValueAccessor, IEquatable<ObjectAccessor<T>>
{
	protected T m_Object;

	private bool m_ReadOnly;

	public Type valueType => m_Object.GetType();

	public IValueAccessor parent => null;

	public ObjectAccessor(T obj, bool readOnly = true)
	{
		m_Object = obj;
		m_ReadOnly = readOnly;
	}

	public virtual object GetValue()
	{
		return GetTypedValue();
	}

	public virtual void SetValue(object value)
	{
		SetTypedValue((T)value);
	}

	public T GetTypedValue()
	{
		return m_Object;
	}

	public void SetTypedValue(T value)
	{
		if (m_ReadOnly)
		{
			throw new InvalidOperationException("ObjectAccessor is readonly");
		}
		m_Object = value;
	}

	public bool Equals(ObjectAccessor<T> other)
	{
		if (other == null)
		{
			return false;
		}
		if (this == other)
		{
			return true;
		}
		return object.Equals(m_Object, other.m_Object);
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
		return Equals((ObjectAccessor<T>)obj);
	}

	public override int GetHashCode()
	{
		return m_Object.GetHashCode();
	}
}
