using System;
using System.Collections;
using Colossal.Annotations;

namespace Game.Reflection;

public class ListElementAccessor<T> : IValueAccessor, IEquatable<ListElementAccessor<T>> where T : IList
{
	[NotNull]
	private readonly ITypedValueAccessor<T> m_Parent;

	private readonly Type m_ElementType;

	private readonly int m_Index;

	public Type valueType => m_ElementType;

	public IValueAccessor parent => m_Parent;

	public ListElementAccessor([NotNull] ITypedValueAccessor<T> parent, [NotNull] Type elementType, int index)
	{
		m_Parent = parent ?? throw new ArgumentNullException("parent");
		m_ElementType = elementType ?? throw new ArgumentNullException("elementType");
		m_Index = index;
	}

	public object GetValue()
	{
		return m_Parent.GetTypedValue()[m_Index];
	}

	public void SetValue(object value)
	{
		m_Parent.GetTypedValue()[m_Index] = value;
	}

	public bool Equals(ListElementAccessor<T> other)
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
			return m_Index == other.m_Index;
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
		return Equals((ListElementAccessor<T>)obj);
	}

	public override int GetHashCode()
	{
		return (m_Parent.GetHashCode() * 397) ^ m_Index;
	}
}
