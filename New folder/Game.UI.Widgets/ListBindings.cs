using System.Collections.Generic;
using Colossal.UI.Binding;
using UnityEngine;

namespace Game.UI.Widgets;

public class ListBindings : IWidgetBindingFactory
{
	public IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged)
	{
		yield return new TriggerBinding<IWidget>(group, "addListElement", delegate(IWidget widget)
		{
			if (widget is IListWidget listWidget)
			{
				listWidget.AddElement();
				onValueChanged(widget);
			}
			else
			{
				UnityEngine.Debug.LogError((widget != null) ? "Widget does not implement IListContainer" : "Invalid widget path");
			}
		}, pathResolver);
		yield return new TriggerBinding<IWidget, int>(group, "duplicateListElement", delegate(IWidget widget, int index)
		{
			if (widget is IListWidget listWidget)
			{
				listWidget.DuplicateElement(index);
				onValueChanged(widget);
			}
			else
			{
				UnityEngine.Debug.LogError((widget != null) ? "Widget does not implement IListContainer" : "Invalid widget path");
			}
		}, pathResolver);
		yield return new TriggerBinding<IWidget, int, int>(group, "moveListElement", delegate(IWidget widget, int fromIndex, int toIndex)
		{
			if (widget is IListWidget listWidget)
			{
				listWidget.MoveElement(fromIndex, toIndex);
				onValueChanged(widget);
			}
			else
			{
				UnityEngine.Debug.LogError((widget != null) ? "Widget does not implement IListContainer" : "Invalid widget path");
			}
		}, pathResolver);
		yield return new TriggerBinding<IWidget, int>(group, "deleteListElement", delegate(IWidget widget, int index)
		{
			if (widget is IListWidget listWidget)
			{
				listWidget.DeleteElement(index);
				onValueChanged(widget);
			}
			else
			{
				UnityEngine.Debug.LogError((widget != null) ? "Widget does not implement IListContainer" : "Invalid widget path");
			}
		}, pathResolver);
		yield return new TriggerBinding<IWidget>(group, "clearList", delegate(IWidget widget)
		{
			if (widget is IListWidget listWidget)
			{
				listWidget.Clear();
				onValueChanged(widget);
			}
			else
			{
				UnityEngine.Debug.LogError((widget != null) ? "Widget does not implement IListContainer" : "Invalid widget path");
			}
		}, pathResolver);
	}
}
