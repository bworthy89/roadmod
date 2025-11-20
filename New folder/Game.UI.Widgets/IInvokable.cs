using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public interface IInvokable : IWidget, IJsonWritable
{
	void Invoke();
}
