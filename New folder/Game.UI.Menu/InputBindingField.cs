using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game.Input;
using Game.UI.Widgets;
using Unity.Entities;

namespace Game.UI.Menu;

public class InputBindingField : Field<ProxyBinding>, IWarning
{
	public class Bindings : IWidgetBindingFactory
	{
		public IEnumerable<IBinding> CreateBindings(string group, IReader<IWidget> pathResolver, ValueChangedCallback onValueChanged)
		{
			yield return new TriggerBinding<IWidget>(group, "rebindInput", delegate(IWidget widget)
			{
				InputBindingField bindingField = widget as InputBindingField;
				if (bindingField != null)
				{
					World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<InputRebindingUISystem>().Start(bindingField.m_Value, delegate(ProxyBinding value)
					{
						bindingField.SetValue(value);
					});
				}
			}, pathResolver);
			yield return new TriggerBinding<IWidget>(group, "unsetInputBinding", delegate(IWidget widget)
			{
				if (widget is InputBindingField inputBindingField)
				{
					ProxyBinding value = inputBindingField.m_Value.Copy();
					value.path = string.Empty;
					value.modifiers = Array.Empty<ProxyModifier>();
					inputBindingField.SetValue(value);
					onValueChanged(widget);
				}
			}, pathResolver);
			yield return new TriggerBinding<IWidget>(group, "resetInputBinding", delegate(IWidget widget)
			{
				InputBindingField bindingField = widget as InputBindingField;
				if (bindingField != null)
				{
					ProxyBinding newBinding = bindingField.m_Value.original.Copy();
					newBinding.ResetConflictCache();
					World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<InputRebindingUISystem>().Start(bindingField.m_Value, newBinding, delegate(ProxyBinding value)
					{
						bindingField.SetValue(value);
					});
				}
			}, pathResolver);
		}
	}

	public bool warning
	{
		get
		{
			return ((uint)m_Value.hasConflicts & (uint)(m_Value.isBuiltIn ? 1 : 3)) != 0;
		}
		set
		{
			throw new NotSupportedException("warning cannot be set to InputBindingField");
		}
	}

	public InputBindingField()
	{
		base.valueWriter = new ValueWriter<ProxyBinding>();
	}

	protected override bool ValueEquals(ProxyBinding newValue, ProxyBinding oldValue)
	{
		return ProxyBinding.pathAndModifiersComparer.Equals(newValue, oldValue);
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("conflicts");
		writer.Write(m_Value.conflicts);
	}
}
