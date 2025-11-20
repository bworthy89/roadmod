using System.Collections.Generic;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public interface IWidgetBindingFactory
{
	IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged);
}
