using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public interface IMutable<out T> : IWidget, IJsonWritable where T : class
{
	T GetValue();
}
