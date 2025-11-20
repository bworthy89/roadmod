using System;
using System.Diagnostics;

namespace Game;

[DebuggerDisplay("{m_Value} ({m_OverrideState})")]
public class OverridableProperty<T>
{
	public const string k_DebuggerDisplay = "{m_Value} ({m_OverrideState})";

	private readonly Func<T> m_SynchronizeFunc;

	private T m_Value;

	private T m_OverrideValue;

	private bool m_OverrideState;

	public T value
	{
		get
		{
			return m_Value;
		}
		set
		{
			m_Value = value;
		}
	}

	public T overrideValue
	{
		get
		{
			return m_OverrideValue;
		}
		set
		{
			m_OverrideState = true;
			m_OverrideValue = value;
		}
	}

	public bool overrideState
	{
		get
		{
			return m_OverrideState;
		}
		set
		{
			m_OverrideState = value;
		}
	}

	public OverridableProperty(Func<T> synchronizeFunc = null)
		: this(default(T), overrideState: false)
	{
		m_SynchronizeFunc = synchronizeFunc;
	}

	private OverridableProperty(T value, bool overrideState)
	{
		m_Value = value;
		m_OverrideState = overrideState;
	}

	public static implicit operator T(OverridableProperty<T> prop)
	{
		if (!prop.m_OverrideState)
		{
			if (prop.m_SynchronizeFunc == null)
			{
				return prop.m_Value;
			}
			return prop.m_SynchronizeFunc();
		}
		return prop.m_OverrideValue;
	}

	public override string ToString()
	{
		return ((T)this/*cast due to .constrained prefix*/).ToString();
	}
}
