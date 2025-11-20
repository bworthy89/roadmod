using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input;

[CreateAssetMenu(menuName = "Colossal/UI/UIInputCombinedAction")]
public class UIInputCombinedAction : UIBaseInputAction
{
	public class State : IProxyAction, IState
	{
		private readonly UIInputAction.State[] m_States;

		public IReadOnlyList<ProxyAction> actions => m_States.Select((UIInputAction.State s) => s.action).ToArray();

		public bool shouldBeEnabled
		{
			get
			{
				return m_States.Any((UIInputAction.State a) => a.shouldBeEnabled);
			}
			set
			{
				UIInputAction.State[] states = m_States;
				for (int i = 0; i < states.Length; i++)
				{
					states[i].shouldBeEnabled = value;
				}
			}
		}

		public bool enabled => m_States.Any((UIInputAction.State a) => ((IProxyAction)a).enabled);

		public event Action<ProxyAction, InputActionPhase> onInteraction
		{
			add
			{
				UIInputAction.State[] states = m_States;
				for (int i = 0; i < states.Length; i++)
				{
					states[i].onInteraction += value;
				}
			}
			remove
			{
				UIInputAction.State[] states = m_States;
				for (int i = 0; i < states.Length; i++)
				{
					states[i].onInteraction -= value;
				}
			}
		}

		public State(params UIInputAction.State[] states)
		{
			m_States = states;
		}

		public bool WasPressedThisFrame()
		{
			if (shouldBeEnabled)
			{
				return m_States.Any((UIInputAction.State s) => s.WasPressedThisFrame());
			}
			return false;
		}

		public bool WasReleasedThisFrame()
		{
			if (shouldBeEnabled)
			{
				return m_States.Any((UIInputAction.State s) => s.WasReleasedThisFrame());
			}
			return false;
		}

		public bool IsPressed()
		{
			if (shouldBeEnabled)
			{
				return m_States.Any((UIInputAction.State s) => s.IsPressed());
			}
			return false;
		}

		public bool IsInProgress()
		{
			if (shouldBeEnabled)
			{
				return m_States.Any((UIInputAction.State s) => s.IsInProgress());
			}
			return false;
		}

		public float GetMagnitude()
		{
			float num = 0f;
			if (!shouldBeEnabled)
			{
				return num;
			}
			UIInputAction.State[] states = m_States;
			foreach (UIInputAction.State state in states)
			{
				num = Mathf.Max(num, state.GetMagnitude());
			}
			return num;
		}

		public T ReadValue<T>() where T : struct
		{
			if (!shouldBeEnabled || m_States.Length == 0)
			{
				return default(T);
			}
			int num = 0;
			float num2 = 0f;
			for (int i = 0; i < m_States.Length; i++)
			{
				float magnitude = m_States[i].GetMagnitude();
				if (magnitude > num2)
				{
					num2 = magnitude;
					num = i;
				}
			}
			return m_States[num].ReadValue<T>();
		}

		public void Dispose()
		{
			UIInputAction.State[] states = m_States;
			for (int i = 0; i < states.Length; i++)
			{
				states[i].Dispose();
			}
		}
	}

	public UIInputActionPart[] m_Parts;

	public override IReadOnlyList<UIInputActionPart> actionParts => m_Parts;

	public override IProxyAction GetState(string source)
	{
		UIInputAction.State[] array = new UIInputAction.State[m_Parts.Length];
		for (int i = 0; i < m_Parts.Length; i++)
		{
			array[i] = new UIInputAction.State(source, m_Parts[i].GetProxyAction(), GetDisplayName(m_Parts[i], source), m_Parts[i].m_Mask);
		}
		return new State(array);
	}

	public override IProxyAction GetState(string source, DisplayGetter displayNameGetter)
	{
		if (m_Parts.Length == 1)
		{
			ProxyAction proxyAction = m_Parts[0].GetProxyAction();
			DisplayNameOverride displayName = displayNameGetter(source, proxyAction, m_Parts[0].m_Mask, m_Parts[0].m_Transform);
			return new UIInputAction.State(source, proxyAction, displayName, m_Parts[0].m_Mask);
		}
		UIInputAction.State[] array = new UIInputAction.State[m_Parts.Length];
		for (int i = 0; i < m_Parts.Length; i++)
		{
			ProxyAction proxyAction2 = m_Parts[i].GetProxyAction();
			DisplayNameOverride displayName2 = displayNameGetter(source, proxyAction2, m_Parts[0].m_Mask, m_Parts[i].m_Transform);
			array[i] = new UIInputAction.State(source, proxyAction2, displayName2, m_Parts[i].m_Mask);
		}
		return new State(array);
	}
}
