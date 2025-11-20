using System.Collections.Generic;
using Colossal.UI.Binding;
using UnityEngine;

namespace Game.UI.Widgets;

public class PagedBindings : IWidgetBindingFactory
{
	public IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged)
	{
		yield return new TriggerBinding<IWidget, int>(group, "setCurrentPageIndex", delegate(IWidget widget, int pageIndex)
		{
			if (widget is IPaged paged)
			{
				paged.currentPageIndex = pageIndex;
			}
			else
			{
				UnityEngine.Debug.LogError((widget != null) ? "Widget does not implement IPaged" : "Invalid widget path");
			}
		}, pathResolver);
	}
}
