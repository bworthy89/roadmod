using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Input;

[CreateAssetMenu(menuName = "Colossal/UI/UIInputAction")]
public class UIInputAction : UIBaseInputAction
{
	public class State : IProxyAction, IState
	{
		private readonly ProxyAction m_Action;

		private readonly InputActivator m_Activator;

		private readonly DisplayNameOverride m_DisplayName;

		public ProxyAction action => m_Action;

		public DisplayNameOverride displayName => m_DisplayName;

		public IReadOnlyList<ProxyAction> actions => new ProxyAction[1] { m_Action };

		public bool shouldBeEnabled
		{
			get
			{
				return m_Activator.enabled;
			}
			set
			{
				m_Activator.enabled = value;
				if (m_DisplayName != null)
				{
					m_DisplayName.active = value;
				}
			}
		}

		public bool enabled => m_Action.enabled;

		public event Action<ProxyAction, InputActionPhase> onInteraction
		{
			add
			{
				m_Action.onInteraction += value;
			}
			remove
			{
				m_Action.onInteraction -= value;
			}
		}

		internal State(string source, ProxyAction action, DisplayNameOverride displayName, InputManager.DeviceType mask)
		{
			m_Action = action ?? throw new ArgumentNullException("action");
			m_Activator = new InputActivator(ignoreIsBuiltIn: true, source, action, mask);
			m_DisplayName = displayName;
		}

		public bool WasPressedThisFrame()
		{
			if (shouldBeEnabled)
			{
				return m_Action.WasPressedThisFrame();
			}
			return false;
		}

		public bool WasReleasedThisFrame()
		{
			if (shouldBeEnabled)
			{
				return m_Action.WasReleasedThisFrame();
			}
			return false;
		}

		public bool IsPressed()
		{
			if (shouldBeEnabled)
			{
				return m_Action.IsPressed();
			}
			return false;
		}

		public bool IsInProgress()
		{
			if (shouldBeEnabled)
			{
				return m_Action.IsInProgress();
			}
			return false;
		}

		public float GetMagnitude()
		{
			if (!shouldBeEnabled)
			{
				return 0f;
			}
			return m_Action.GetMagnitude();
		}

		public T ReadValue<T>() where T : struct
		{
			if (!shouldBeEnabled)
			{
				return default(T);
			}
			return m_Action.ReadValue<T>();
		}

		public void Dispose()
		{
			m_Activator?.Dispose();
			m_DisplayName?.Dispose();
		}
	}

	public InputActionReference m_Action;

	public ProcessAs m_ProcessAs;

	public Transform m_Transform;

	public InputManager.DeviceType m_Mask = InputManager.DeviceType.All;

	[NonSerialized]
	private UIInputActionPart[] m_ActionParts;

	public override IReadOnlyList<UIInputActionPart> actionParts
	{
		get
		{
			UIInputActionPart[] array = m_ActionParts;
			if (array == null)
			{
				UIInputActionPart[] obj = new UIInputActionPart[1]
				{
					new UIInputActionPart
					{
						m_Action = m_Action,
						m_ProcessAs = m_ProcessAs,
						m_Transform = m_Transform,
						m_Mask = m_Mask
					}
				};
				UIInputActionPart[] array2 = obj;
				m_ActionParts = obj;
				array = array2;
			}
			return array;
		}
	}

	public override IProxyAction GetState(string source)
	{
		ProxyAction action = InputManager.instance.FindAction(m_Action.action);
		DisplayNameOverride displayName = new DisplayNameOverride(source, action, m_AliasName, base.displayPriority, m_Transform);
		return new State(source, action, displayName, m_Mask);
	}

	public override IProxyAction GetState(string source, DisplayGetter displayNameGetter)
	{
		ProxyAction action = InputManager.instance.FindAction(m_Action.action);
		DisplayNameOverride displayName = displayNameGetter(source, action, m_Mask, m_Transform);
		return new State(source, action, displayName, m_Mask);
	}
}
