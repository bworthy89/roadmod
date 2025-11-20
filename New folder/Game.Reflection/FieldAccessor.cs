using System;
using System.Reflection;
using Colossal.Annotations;

namespace Game.Reflection;

public class FieldAccessor : IValueAccessor, IEquatable<FieldAccessor>
{
	[NotNull]
	private readonly IValueAccessor m_Parent;

	[NotNull]
	private readonly FieldInfo m_Field;

	public Type valueType => m_Field.FieldType;

	public IValueAccessor parent => m_Parent;

	public FieldAccessor([NotNull] IValueAccessor parent, [NotNull] FieldInfo field)
	{
		m_Parent = parent ?? throw new ArgumentNullException("parent");
		m_Field = field ?? throw new ArgumentNullException("field");
	}

	public object GetValue()
	{
		object value = m_Parent.GetValue();
		return m_Field.GetValue(value);
	}

	public void SetValue(object value)
	{
		object value2 = m_Parent.GetValue();
		m_Field.SetValue(value2, value);
		if (m_Parent.valueType.IsValueType)
		{
			m_Parent.SetValue(value2);
		}
	}

	public bool Equals(FieldAccessor other)
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
			return m_Field.Equals(other.m_Field);
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
		return Equals((FieldAccessor)obj);
	}

	public override int GetHashCode()
	{
		return (m_Parent.GetHashCode() * 397) ^ m_Field.GetHashCode();
	}
}
