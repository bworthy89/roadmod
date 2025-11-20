using System.Collections.Generic;
using Colossal.UI.Binding;
using UnityEngine;

namespace Game.UI.Widgets;

public class SettableBindings : IWidgetBindingFactory
{
	public IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged)
	{
		yield return new RawTriggerBinding(group, "setValue", delegate(IJsonReader reader)
		{
			pathResolver.Read(reader, out var value);
			if (value is ISettable settable)
			{
				settable.SetValue(reader);
				if (settable.shouldTriggerValueChangedEvent)
				{
					onValueChanged(value);
				}
			}
			else
			{
				reader.SkipValue();
				UnityEngine.Debug.LogError((value != null) ? "Widget does not implement ISettable" : "Invalid widget path");
			}
		});
	}
}
