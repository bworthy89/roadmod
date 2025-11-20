using System.Collections.Generic;
using Colossal.UI.Binding;
using UnityEngine;

namespace Game.UI.Widgets;

public class InvokableBindings : IWidgetBindingFactory
{
	public IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged)
	{
		yield return new TriggerBinding<IWidget>(group, "invoke", delegate(IWidget widget)
		{
			if (widget is IInvokable invokable)
			{
				invokable.Invoke();
			}
			else
			{
				UnityEngine.Debug.LogError((widget != null) ? "Widget does not implement IInvokable" : "Invalid widget path");
			}
		}, pathResolver);
	}
}
