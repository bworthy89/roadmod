using System;
using System.Reflection;
using Colossal.Annotations;
using Unity.Jobs;

namespace Game.Reflection;

public class GetterWithDepsAccessor : IValueAccessor, IEquatable<GetterWithDepsAccessor>
{
	[NotNull]
	private readonly IValueAccessor m_Parent;

	[NotNull]
	private readonly MethodInfo m_Getter;

	[CanBeNull]
	private readonly object[] m_Parameters;

	private readonly int m_DepsIndex;

	public Type valueType => m_Getter.ReturnType;

	public IValueAccessor parent => m_Parent;

	public GetterWithDepsAccessor([NotNull] IValueAccessor parent, [NotNull] MethodInfo getter, [CanBeNull] object[] parameters = null, int depsIndex = -1)
	{
		m_Parent = parent ?? throw new ArgumentNullException("parent");
		m_Getter = getter ?? throw new ArgumentNullException("getter");
		m_Parameters = parameters;
		m_DepsIndex = depsIndex;
	}

	public object GetValue()
	{
		object value = m_Parent.GetValue();
		object result = m_Getter.Invoke(value, m_Parameters);
		if (m_DepsIndex != -1 && m_Parameters != null)
		{
			((JobHandle)m_Parameters[m_DepsIndex]).Complete();
		}
		return result;
	}

	public void SetValue(object value)
	{
		throw new InvalidOperationException("GetterWithDepsAccessor is readonly");
	}

	public bool Equals(GetterWithDepsAccessor other)
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
		return Equals((GetterWithDepsAccessor)obj);
	}

	public override int GetHashCode()
	{
		return (m_Parent.GetHashCode() * 397) ^ m_Getter.GetHashCode();
	}
}
