using System;

namespace Game.Rendering.Utilities;

public abstract class State
{
	public enum ResultType
	{
		Continue,
		Stop,
		Transition
	}

	public struct Result
	{
		public ResultType type;

		public State next;

		public bool isContinue => type == ResultType.Continue;

		public bool isStop => type == ResultType.Stop;

		public bool isTransition => type == ResultType.Transition;

		public static Result Continue => new Result
		{
			type = ResultType.Continue,
			next = null
		};

		public static Result Stop => new Result
		{
			type = ResultType.Stop,
			next = null
		};

		public static Result TransitionTo(State state)
		{
			return new Result
			{
				type = ResultType.Transition,
				next = state
			};
		}
	}

	private StateMachine _machine;

	public StateMachine machine
	{
		get
		{
			return _machine;
		}
		set
		{
			if (_machine != null)
			{
				throw new Exception("property is read only after first set");
			}
			_machine = value;
		}
	}

	public string Name { get; set; }

	public State()
	{
	}

	public virtual void TransitionIn()
	{
	}

	public virtual Result Update()
	{
		return Result.Continue;
	}

	public virtual void LateUpdate()
	{
	}

	public virtual void TransitionOut()
	{
		_machine = null;
	}
}
