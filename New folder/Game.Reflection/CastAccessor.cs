using System;
using Colossal.Annotations;

namespace Game.Reflection;

public class CastAccessor<T> : ITypedValueAccessor<T>, IValueAccessor, IEquatable<CastAccessor<T>>
{
	[NotNull]
	private readonly IValueAccessor m_Accessor;

	[NotNull]
	private readonly Converter<object, T> m_FromObject;

	[NotNull]
	private readonly Converter<T, object> m_ToObject;

	public Type valueType => typeof(T);

	public IValueAccessor parent => m_Accessor;

	public CastAccessor([NotNull] IValueAccessor accessor)
		: this(accessor, (Converter<object, T>)FromObject, (Converter<T, object>)ToObject)
	{
	}

	public CastAccessor([NotNull] IValueAccessor accessor, [NotNull] Converter<object, T> fromObject, [NotNull] Converter<T, object> toObject)
	{
		m_Accessor = accessor ?? throw new ArgumentNullException("accessor");
		m_FromObject = fromObject ?? throw new ArgumentNullException("fromObject");
		m_ToObject = toObject ?? throw new ArgumentNullException("toObject");
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
		return m_FromObject(m_Accessor.GetValue());
	}

	public void SetTypedValue(T value)
	{
		m_Accessor.SetValue(m_ToObject(value));
	}

	public bool Equals(CastAccessor<T> other)
	{
		if (other == null)
		{
			return false;
		}
		if (this == other)
		{
			return true;
		}
		return m_Accessor.Equals(other.m_Accessor);
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
		return Equals((CastAccessor<T>)obj);
	}

	public override int GetHashCode()
	{
		return m_Accessor.GetHashCode();
	}

	private static T FromObject(object value)
	{
		return (T)value;
	}

	private static object ToObject(T value)
	{
		return value;
	}
}
