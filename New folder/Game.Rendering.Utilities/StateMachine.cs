using System;

namespace Game.Rendering.Utilities;

public class StateMachine
{
	public Action onStarted;

	public Action onStopped;

	public Action<State> onTransitioned;

	private string m_name;

	private State _currentState;

	public string name => m_name;

	public bool isStarted => _currentState != null;

	public StateMachine(string name = null)
	{
		m_name = name;
	}

	public virtual void Start(State initialState)
	{
		if (!isStarted && initialState != null)
		{
			TransitionTo(initialState);
			onStarted.Fire();
			return;
		}
		throw new Exception("already started");
	}

	public virtual void Stop()
	{
		if (isStarted)
		{
			if (_currentState != null)
			{
				_currentState.TransitionOut();
				_currentState = null;
			}
			onStopped.Fire();
		}
	}

	public void Update()
	{
		if (isStarted)
		{
			State.Result result = _currentState.Update();
			switch (result.type)
			{
			case State.ResultType.Stop:
				Stop();
				break;
			case State.ResultType.Transition:
				TransitionTo(result.next);
				break;
			case State.ResultType.Continue:
				break;
			}
		}
	}

	public void LateUpdate()
	{
		if (isStarted)
		{
			_currentState.LateUpdate();
		}
	}

	public string GetCurrentStateName()
	{
		if (_currentState != null)
		{
			if (!string.IsNullOrEmpty(_currentState.Name))
			{
				return _currentState.Name;
			}
			return _currentState.ToString();
		}
		return "none";
	}

	public bool IsIn<T>() where T : State
	{
		if (_currentState != null)
		{
			return _currentState is T;
		}
		return false;
	}

	private void TransitionTo(State state)
	{
		_currentState?.TransitionOut();
		_currentState = state;
		_currentState.machine = this;
		_currentState.TransitionIn();
		onTransitioned.Fire(_currentState);
		Update();
	}
}
