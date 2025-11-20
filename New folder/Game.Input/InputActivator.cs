using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Input;

public class InputActivator : IDisposable
{
	private readonly ProxyAction[] m_Actions;

	private readonly string m_Name;

	private bool m_Enabled;

	private InputManager.DeviceType m_Mask;

	private bool m_Disposed;

	public string name => m_Name;

	public IReadOnlyList<ProxyAction> actions => m_Actions;

	public bool enabled
	{
		get
		{
			return m_Enabled;
		}
		set
		{
			if (!m_Disposed && value != m_Enabled)
			{
				m_Enabled = value;
				Update();
			}
		}
	}

	public InputManager.DeviceType mask
	{
		get
		{
			return m_Mask;
		}
		set
		{
			if (!m_Disposed && value != m_Mask)
			{
				m_Mask = value;
				Update();
			}
		}
	}

	public InputActivator(string activatorName, ProxyAction action, InputManager.DeviceType mask = InputManager.DeviceType.All, bool enabled = false)
		: this(ignoreIsBuiltIn: false, activatorName, action, mask, enabled)
	{
	}

	internal InputActivator(bool ignoreIsBuiltIn, string activatorName, ProxyAction action, InputManager.DeviceType mask = InputManager.DeviceType.All, bool enabled = false)
	{
		if (action == null)
		{
			throw new ArgumentNullException("action");
		}
		if (!ignoreIsBuiltIn && action.isBuiltIn)
		{
			throw new ArgumentException("Activator can not be created for built-in action");
		}
		m_Name = activatorName ?? "InputActivator";
		m_Actions = new ProxyAction[1] { action };
		m_Mask = mask;
		action.m_Activators.Add(this);
		this.enabled = enabled;
	}

	public InputActivator(string activatorName, IList<ProxyAction> actions, InputManager.DeviceType mask = InputManager.DeviceType.All, bool enabled = false)
		: this(ignoreIsBuiltIn: false, activatorName, actions, mask, enabled)
	{
	}

	internal InputActivator(bool ignoreIsBuiltIn, string activatorName, IList<ProxyAction> actions, InputManager.DeviceType mask = InputManager.DeviceType.All, bool enabled = false)
	{
		if (actions == null)
		{
			throw new ArgumentNullException("actions");
		}
		m_Actions = actions.Where((ProxyAction a) => a != null).Distinct().ToArray();
		if (!ignoreIsBuiltIn && m_Actions.Any((ProxyAction a) => a.isBuiltIn))
		{
			throw new ArgumentException("Activator can not be created for built-in action");
		}
		m_Name = activatorName ?? "InputActivator";
		m_Mask = mask;
		ProxyAction[] array = m_Actions;
		for (int num = 0; num < array.Length; num++)
		{
			array[num].m_Activators.Add(this);
		}
		this.enabled = enabled;
	}

	private void Update()
	{
		ProxyAction[] array = m_Actions;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].UpdateState();
		}
	}

	public void Dispose()
	{
		if (!m_Disposed)
		{
			m_Disposed = true;
			ProxyAction[] array = m_Actions;
			foreach (ProxyAction obj in array)
			{
				obj.m_Activators.Remove(this);
				obj.UpdateState();
			}
		}
	}
}
