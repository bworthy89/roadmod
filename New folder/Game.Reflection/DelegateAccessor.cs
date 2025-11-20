using System;
using Colossal.Annotations;

namespace Game.Reflection;

public class DelegateAccessor<T> : ITypedValueAccessor<T>, IValueAccessor
{
	[NotNull]
	private readonly Func<T> m_Getter;

	[CanBeNull]
	private readonly Action<T> m_Setter;

	public Type valueType => typeof(T);

	public IValueAccessor parent => null;

	public DelegateAccessor([NotNull] Func<T> getter, [CanBeNull] Action<T> setter = null)
	{
		m_Getter = getter ?? throw new ArgumentNullException("getter");
		m_Setter = setter;
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
		return m_Getter();
	}

	public void SetTypedValue(T value)
	{
		if (m_Setter != null)
		{
			m_Setter(value);
			return;
		}
		throw new InvalidOperationException("DelegateAccessor is readonly");
	}
}
