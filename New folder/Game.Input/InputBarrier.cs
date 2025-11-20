using System;
using System.Collections.Generic;
using System.Linq;

namespace Game.Input;

public class InputBarrier : IDisposable
{
	private readonly ProxyActionMap[] m_Maps;

	private readonly ProxyAction[] m_Actions;

	private readonly string m_Name;

	private bool m_Blocked;

	private InputManager.DeviceType m_Mask;

	private bool m_Disposed;

	public string name => m_Name;

	public IReadOnlyList<ProxyActionMap> maps => m_Maps;

	public IReadOnlyList<ProxyAction> actions => m_Actions;

	public bool blocked
	{
		get
		{
			return m_Blocked;
		}
		set
		{
			if (!m_Disposed && value != m_Blocked)
			{
				m_Blocked = value;
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

	public InputBarrier(string barrierName, ProxyActionMap map, InputManager.DeviceType mask = InputManager.DeviceType.All, bool blocked = false)
	{
		m_Name = barrierName ?? "InputBarrier";
		m_Maps = new ProxyActionMap[1] { map ?? throw new ArgumentNullException("map") };
		m_Actions = Array.Empty<ProxyAction>();
		m_Mask = mask;
		map.m_Barriers.Add(this);
		this.blocked = blocked;
	}

	public InputBarrier(string barrierName, ProxyAction action, InputManager.DeviceType mask = InputManager.DeviceType.All, bool blocked = false)
	{
		m_Name = barrierName ?? "InputBarrier";
		m_Actions = new ProxyAction[1] { action ?? throw new ArgumentNullException("action") };
		m_Maps = Array.Empty<ProxyActionMap>();
		m_Mask = mask;
		action.m_Barriers.Add(this);
		this.blocked = blocked;
	}

	public InputBarrier(string barrierName, IList<ProxyActionMap> maps, IList<ProxyAction> actions, InputManager.DeviceType mask = InputManager.DeviceType.All, bool blocked = false)
	{
		if (maps == null)
		{
			throw new ArgumentNullException("maps");
		}
		if (actions == null)
		{
			throw new ArgumentNullException("actions");
		}
		m_Name = barrierName ?? "InputBarrier";
		m_Maps = maps.Where((ProxyActionMap m) => m != null).Distinct().ToArray();
		m_Actions = actions.Where((ProxyAction a) => a != null).Distinct().ToArray();
		m_Mask = mask;
		ProxyActionMap[] array = m_Maps;
		for (int num = 0; num < array.Length; num++)
		{
			array[num].m_Barriers.Add(this);
		}
		ProxyAction[] array2 = m_Actions;
		for (int num = 0; num < array2.Length; num++)
		{
			array2[num].m_Barriers.Add(this);
		}
		this.blocked = blocked;
	}

	public InputBarrier(string barrierName, IList<ProxyActionMap> maps, InputManager.DeviceType mask = InputManager.DeviceType.All, bool blocked = false)
		: this(barrierName, maps, Array.Empty<ProxyAction>(), mask, blocked)
	{
	}

	public InputBarrier(string barrierName, IList<ProxyAction> actions, InputManager.DeviceType mask = InputManager.DeviceType.All, bool blocked = false)
		: this(barrierName, Array.Empty<ProxyActionMap>(), actions, mask, blocked)
	{
	}

	private void Update()
	{
		ProxyActionMap[] array = m_Maps;
		for (int i = 0; i < array.Length; i++)
		{
			array[i].UpdateState();
		}
		ProxyAction[] array2 = m_Actions;
		for (int i = 0; i < array2.Length; i++)
		{
			array2[i].UpdateState();
		}
	}

	public void Dispose()
	{
		if (!m_Disposed)
		{
			m_Disposed = true;
			ProxyActionMap[] array = m_Maps;
			foreach (ProxyActionMap obj in array)
			{
				obj.m_Barriers.Remove(this);
				obj.UpdateState();
			}
			ProxyAction[] array2 = m_Actions;
			foreach (ProxyAction obj2 in array2)
			{
				obj2.m_Barriers.Remove(this);
				obj2.UpdateState();
			}
		}
	}
}
