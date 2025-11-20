using System;

namespace Game.Input;

public class DisplayNameOverride : IDisposable
{
	public const int kDisabledPriority = -1;

	public const int kToolTipPriority = 0;

	private readonly ProxyAction m_Action;

	private readonly string m_Source;

	private bool m_Disposed;

	private int m_Priority;

	private UIBaseInputAction.Transform m_Transform;

	private string m_DisplayName;

	private bool m_Active;

	public string source => m_Source;

	public bool isDisposed => m_Disposed;

	public bool active
	{
		get
		{
			if (m_Active)
			{
				return m_Priority != -1;
			}
			return false;
		}
		set
		{
			if (value != m_Active)
			{
				m_Active = value;
				m_Action.UpdateDisplay();
			}
		}
	}

	public string displayName
	{
		get
		{
			return m_DisplayName;
		}
		set
		{
			if (value != m_DisplayName)
			{
				m_DisplayName = value;
				if (m_Active)
				{
					m_Action.UpdateDisplay();
				}
			}
		}
	}

	public int priority
	{
		get
		{
			return m_Priority;
		}
		set
		{
			if (value != m_Priority)
			{
				m_Priority = value;
				if (m_Active)
				{
					m_Action.UpdateDisplay();
				}
			}
		}
	}

	public UIBaseInputAction.Transform transform
	{
		get
		{
			return m_Transform;
		}
		set
		{
			if (value != m_Transform)
			{
				m_Transform = value;
				if (m_Active)
				{
					m_Action.UpdateDisplay();
				}
			}
		}
	}

	public DisplayNameOverride(string overrideSource, ProxyAction action, string displayName = null, int priority = -1, UIBaseInputAction.Transform transform = UIBaseInputAction.Transform.None)
	{
		m_Action = action ?? throw new ArgumentNullException("action");
		m_Source = overrideSource;
		m_DisplayName = displayName;
		m_Priority = priority;
		m_Transform = transform;
		m_Action.m_DisplayOverrides.Add(this);
	}

	public bool Equals(DisplayNameOverride other)
	{
		if (m_Priority != other.m_Priority)
		{
			return false;
		}
		if (m_DisplayName != other.m_DisplayName)
		{
			return false;
		}
		return true;
	}

	public void Dispose()
	{
		if (!m_Disposed)
		{
			m_Disposed = true;
			m_Action.m_DisplayOverrides.Remove(this);
		}
	}
}
