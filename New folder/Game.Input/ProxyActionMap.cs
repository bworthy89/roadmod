using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Colossal;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Utilities;

namespace Game.Input;

[DebuggerDisplay("{name}")]
public class ProxyActionMap
{
	private readonly InputActionMap m_SourceMap;

	private readonly Dictionary<string, ProxyAction> m_Actions = new Dictionary<string, ProxyAction>();

	internal HashSet<InputBarrier> m_Barriers = new HashSet<InputBarrier>();

	private bool m_Enabled;

	private InputManager.DeviceType m_Mask = InputManager.DeviceType.All;

	internal InputActionMap sourceMap => m_SourceMap;

	public string name => m_SourceMap.name;

	public IReadOnlyDictionary<string, ProxyAction> actions => m_Actions;

	internal IReadOnlyCollection<InputBarrier> barriers => m_Barriers;

	public IEnumerable<ProxyBinding> bindings
	{
		get
		{
			foreach (var (_, proxyAction2) in m_Actions)
			{
				foreach (var (_, proxyComposite2) in proxyAction2.composites)
				{
					foreach (KeyValuePair<ActionComponent, ProxyBinding> binding in proxyComposite2.bindings)
					{
						binding.Deconstruct(out var _, out var value);
						yield return value;
					}
				}
			}
		}
	}

	public bool enabled => m_Enabled;

	public InputManager.DeviceType mask
	{
		get
		{
			return m_Mask;
		}
		internal set
		{
			if (value == m_Mask)
			{
				return;
			}
			m_Mask = value;
			m_SourceMap.bindingMask = value.ToInputBinding();
			foreach (KeyValuePair<string, ProxyAction> action in m_Actions)
			{
				action.Deconstruct(out var _, out var value2);
				value2.UpdateState();
			}
		}
	}

	internal ProxyActionMap(InputActionMap sourceMap)
	{
		m_SourceMap = sourceMap;
	}

	internal void InitActions()
	{
		foreach (InputAction action in sourceMap.actions)
		{
			ProxyAction proxyAction = new ProxyAction(this, action);
			m_Actions.Add(proxyAction.name, proxyAction);
		}
		UpdateState();
	}

	public ProxyAction FindAction(string name)
	{
		return m_Actions.GetValueOrDefault(name);
	}

	internal ProxyAction FindAction(InputAction action)
	{
		return FindAction(action.name);
	}

	public bool TryFindAction(string name, out ProxyAction action)
	{
		return m_Actions.TryGetValue(name, out action);
	}

	public ProxyAction AddAction(ProxyAction.Info actionInfo, bool bulk = false)
	{
		using (Colossal.PerformanceCounter.Start(delegate(TimeSpan t)
		{
			InputManager.log.InfoFormat("Action \"{1}\" added in {0}ms", t.TotalMilliseconds, actionInfo.m_Name);
		}))
		{
			using (InputManager.DeferUpdating())
			{
				if (TryFindAction(actionInfo.m_Name, out var action))
				{
					return action;
				}
				InputAction inputAction = m_SourceMap.AddAction(actionInfo.m_Name, actionInfo.m_Type.GetInputActionType(), null, null, null, null, actionInfo.m_Type.GetExpectedControlLayout());
				foreach (ProxyComposite.Info composite in actionInfo.m_Composites)
				{
					InputManager.instance.CreateCompositeBinding(inputAction, composite);
				}
				action = new ProxyAction(this, inputAction);
				m_Actions.Add(action.name, action);
				InputManager.instance.InitializeMasks(action);
				return action;
			}
		}
	}

	internal void UpdateState()
	{
		bool flag = m_Barriers.All((InputBarrier b) => !b.blocked);
		if (flag == m_Enabled)
		{
			return;
		}
		m_Enabled = flag;
		foreach (KeyValuePair<string, ProxyAction> action in m_Actions)
		{
			action.Deconstruct(out var _, out var value);
			value.UpdateState();
		}
	}
}
