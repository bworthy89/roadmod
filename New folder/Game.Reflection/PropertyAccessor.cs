using System;
using System.Reflection;
using Colossal.Annotations;

namespace Game.Reflection;

public class PropertyAccessor : IValueAccessor, IEquatable<PropertyAccessor>
{
	[NotNull]
	private readonly IValueAccessor m_Parent;

	[NotNull]
	private readonly MethodInfo m_Getter;

	[CanBeNull]
	private readonly MethodInfo m_Setter;

	public Type valueType => m_Getter.ReturnType;

	public IValueAccessor parent => m_Parent;

	public PropertyAccessor([NotNull] IValueAccessor parent, [NotNull] MethodInfo getter, [CanBeNull] MethodInfo setter)
	{
		m_Parent = parent ?? throw new ArgumentNullException("parent");
		m_Getter = getter ?? throw new ArgumentNullException("getter");
		m_Setter = setter;
	}

	public object GetValue()
	{
		object value = m_Parent.GetValue();
		return m_Getter.Invoke(value, null);
	}

	public void SetValue(object value)
	{
		if (m_Setter != null)
		{
			object value2 = m_Parent.GetValue();
			m_Setter.Invoke(value2, new object[1] { value });
		}
	}

	public bool Equals(PropertyAccessor other)
	{
		if (other == null)
		{
			return false;
		}
		if (this == other)
		{
			return true;
		}
		if (m_Parent.Equals(other.m_Parent))
		{
			return m_Getter.Equals(other.m_Getter);
		}
		return false;
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
		return Equals((PropertyAccessor)obj);
	}

	public override int GetHashCode()
	{
		return (m_Parent.GetHashCode() * 397) ^ m_Getter.GetHashCode();
	}
}
