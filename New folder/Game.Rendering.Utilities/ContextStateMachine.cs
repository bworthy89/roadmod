namespace Game.Rendering.Utilities;

public class ContextStateMachine<T> : StateMachine
{
	public T Context { get; private set; }

	public ContextStateMachine(T context)
	{
		Context = context;
	}
}
