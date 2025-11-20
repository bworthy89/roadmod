using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.UI.Binding;
using Game.SceneFlow;
using Game.UI.Debug;
using Unity.Entities;
using UnityEngine.Rendering;

namespace Game.Debug;

[DebugContainer]
public static class UIBindingDebugUI
{
	private static void Rebuild()
	{
		DebugSystem.Rebuild(BuildUIBindingsDebugUI);
	}

	private static void AddBindingButtonsRecursive(DebugUISystem debugUIsystem, Dictionary<string, DebugUI.Container> containers, IBindingGroup group)
	{
		foreach (IBinding binding in group.bindings)
		{
			IDebugBinding debugBinding = binding as IDebugBinding;
			if (debugBinding != null)
			{
				if (!containers.TryGetValue(debugBinding.group, out var value))
				{
					value = new DebugUI.Foldout(debugBinding.group, new ObservableList<DebugUI.Widget>());
					containers.Add(debugBinding.group, value);
				}
				if (debugBinding.debugType == DebugBindingType.Trigger)
				{
					value.children.Add(new DebugUI.Button
					{
						displayName = debugBinding.name,
						action = delegate
						{
							debugUIsystem.Trigger(debugBinding);
						}
					});
				}
				else if (debugBinding.debugType == DebugBindingType.Event || debugBinding.debugType == DebugBindingType.Value)
				{
					value.children.Add(new DebugUI.BoolField
					{
						displayName = debugBinding.name,
						getter = () => debugUIsystem.observedBinding == debugBinding,
						setter = delegate(bool v)
						{
							debugUIsystem.observedBinding = (v ? debugBinding : null);
						}
					});
				}
				else
				{
					value.children.Add(new DebugUI.Value
					{
						displayName = debugBinding.name,
						getter = () => FormatGenericTypeString(debugBinding.GetType())
					});
				}
			}
			if (binding is IBindingGroup bindingGroup)
			{
				AddBindingButtonsRecursive(debugUIsystem, containers, bindingGroup);
			}
		}
	}

	private static string FormatGenericTypeString(Type t)
	{
		if (!t.IsGenericType)
		{
			return t.Name;
		}
		string name = t.GetGenericTypeDefinition().Name;
		name = name.Substring(0, name.IndexOf('`'));
		string text = string.Join(",", t.GetGenericArguments().Select(FormatGenericTypeString).ToArray());
		return name + "<" + text + ">";
	}

	[DebugTab("UI Bindings", -970)]
	private static List<DebugUI.Widget> BuildUIBindingsDebugUI(World world)
	{
		DebugUISystem debugUISystem = world.GetOrCreateSystemManaged<DebugUISystem>();
		IBindingRegistry bindings = GameManager.instance.userInterface.bindings;
		Dictionary<string, DebugUI.Container> dictionary = new Dictionary<string, DebugUI.Container>();
		if (bindings != null)
		{
			AddBindingButtonsRecursive(debugUISystem, dictionary, bindings);
		}
		List<DebugUI.Widget> list = new List<DebugUI.Widget>();
		list.Add(new DebugUI.Button
		{
			displayName = "Refresh",
			action = Rebuild
		});
		list.Add(new DebugUI.Button
		{
			displayName = "Clear",
			action = delegate
			{
				debugUISystem.observedBinding = null;
			}
		});
		list.AddRange(dictionary.Values.OrderBy((DebugUI.Container container) => container.displayName));
		return list;
	}
}
