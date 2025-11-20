using System;
using System.Collections.Generic;

namespace Game.Input;

public class InputConflictResolution : IDisposable
{
	private class State
	{
		public readonly ProxyAction m_Action;

		public bool m_HasConflict;

		public bool enabled
		{
			get
			{
				if (m_Action.preResolvedEnable)
				{
					return !m_HasConflict;
				}
				return false;
			}
		}

		public State(ProxyAction action)
		{
			m_Action = action;
			Reset();
		}

		public void Reset()
		{
			m_HasConflict = false;
		}

		public void Apply()
		{
			m_Action.ApplyState(enabled, m_Action.preResolvedMask);
		}
	}

	private bool m_ActionsDirty = true;

	private bool m_ConflictsDirty = true;

	private bool m_UpdateInProgress;

	private List<State> m_SystemActions = new List<State>();

	private List<State> m_UIActions = new List<State>();

	private List<State> m_ModActions = new List<State>();

	public event Action EventActionRefreshed;

	public event Action EventConflictResolved;

	public void Initialize()
	{
		InputManager.instance.EventActionsChanged += OnActionsChanged;
		InputManager.instance.EventPreResolvedActionChanged += OnPreResolvedActionChanged;
		InputManager.instance.EventControlSchemeChanged += OnControlSchemeChanged;
	}

	public void Dispose()
	{
		InputManager.instance.EventActionsChanged -= OnActionsChanged;
		InputManager.instance.EventPreResolvedActionChanged -= OnPreResolvedActionChanged;
		InputManager.instance.EventControlSchemeChanged -= OnControlSchemeChanged;
	}

	public void Update()
	{
		m_UpdateInProgress = true;
		if (m_ActionsDirty)
		{
			RefreshActions();
			m_ActionsDirty = false;
			this.EventActionRefreshed?.Invoke();
		}
		if (m_ConflictsDirty)
		{
			ResolveConflicts();
			m_ConflictsDirty = false;
			this.EventConflictResolved?.Invoke();
		}
		m_UpdateInProgress = false;
	}

	private void OnActionsChanged()
	{
		if (!m_UpdateInProgress)
		{
			m_ActionsDirty = true;
			m_ConflictsDirty = true;
		}
	}

	private void OnControlSchemeChanged(InputManager.ControlScheme scheme)
	{
		if (!m_UpdateInProgress)
		{
			m_ConflictsDirty = true;
		}
	}

	private void OnPreResolvedActionChanged()
	{
		if (!m_UpdateInProgress)
		{
			m_ConflictsDirty = true;
		}
	}

	private void RefreshActions()
	{
		m_SystemActions.Clear();
		m_UIActions.Clear();
		m_ModActions.Clear();
		foreach (ProxyAction action in InputManager.instance.actions)
		{
			if (!action.isBuiltIn)
			{
				m_ModActions.Add(new State(action));
			}
			else if (action.isSystemAction)
			{
				m_SystemActions.Add(new State(action));
			}
			else
			{
				m_UIActions.Add(new State(action));
			}
		}
	}

	private void ResolveConflicts()
	{
		foreach (State uIAction in m_UIActions)
		{
			uIAction.Reset();
		}
		foreach (State modAction in m_ModActions)
		{
			modAction.Reset();
		}
		foreach (State systemAction in m_SystemActions)
		{
			if (!systemAction.enabled)
			{
				continue;
			}
			foreach (State uIAction2 in m_UIActions)
			{
				if (uIAction2.enabled)
				{
					Resolve(systemAction, uIAction2);
				}
			}
			foreach (State modAction2 in m_ModActions)
			{
				if (modAction2.enabled)
				{
					Resolve(systemAction, modAction2);
				}
			}
		}
		foreach (State uIAction3 in m_UIActions)
		{
			if (!uIAction3.enabled)
			{
				continue;
			}
			foreach (State modAction3 in m_ModActions)
			{
				if (modAction3.enabled)
				{
					Resolve(uIAction3, modAction3);
				}
			}
		}
		foreach (State uIAction4 in m_UIActions)
		{
			uIAction4.Apply();
		}
		foreach (State modAction4 in m_ModActions)
		{
			modAction4.Apply();
		}
		static void Resolve(State primary, State secondary)
		{
			if (InputManager.HasConflicts(primary.m_Action, secondary.m_Action, primary.m_Action.preResolvedMask, secondary.m_Action.preResolvedMask))
			{
				secondary.m_HasConflict = true;
			}
		}
	}
}
