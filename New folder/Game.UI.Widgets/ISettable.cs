using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public interface ISettable : IWidget, IJsonWritable
{
	bool shouldTriggerValueChangedEvent { get; }

	void SetValue(IJsonReader reader);
}
