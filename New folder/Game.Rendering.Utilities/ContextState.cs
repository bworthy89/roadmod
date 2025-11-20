namespace Game.Rendering.Utilities;

public class ContextState<T> : State
{
	public T Context => (base.machine as ContextStateMachine<T>).Context;
}
