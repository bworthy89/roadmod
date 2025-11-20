using System.Collections.Generic;
using Colossal.UI.Binding;
using UnityEngine;

namespace Game.UI.Widgets;

public class ExpandableBindings : IWidgetBindingFactory
{
	public IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged)
	{
		yield return new TriggerBinding<IWidget, bool>(group, "setExpanded", delegate(IWidget widget, bool expanded)
		{
			if (widget is IExpandable expandable)
			{
				expandable.expanded = expanded;
			}
			else
			{
				UnityEngine.Debug.LogError((widget != null) ? "Widget does not implement IExpandable" : "Invalid widget path");
			}
		}, pathResolver);
	}
}
