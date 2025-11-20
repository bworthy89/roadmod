using System;
using System.Collections.Generic;
using Colossal.UI.Binding;
using Game.Reflection;
using Game.UI.Localization;

namespace Game.UI.Widgets;

public class PopupValueField<T> : NamedWidgetWithTooltip, IExpandable, IDisableCallback
{
	private bool m_LastExpanded;

	private T m_Value;

	private LocalizedString m_DisplayValue;

	public bool expanded { get; set; }

	public ITypedValueAccessor<T> accessor { get; set; }

	public IValueFieldPopup<T> popup { get; set; }

	public override IList<IWidget> visibleChildren
	{
		get
		{
			if (!expanded)
			{
				return Array.Empty<IWidget>();
			}
			return popup.children;
		}
	}

	public override string propertiesTypeName => "Game.UI.Widgets.PopupValueField";

	protected override WidgetChanges Update()
	{
		WidgetChanges widgetChanges = base.Update();
		if (expanded != m_LastExpanded)
		{
			widgetChanges |= WidgetChanges.Properties | WidgetChanges.Children;
			m_LastExpanded = expanded;
			if (expanded)
			{
				popup.Attach(accessor);
			}
			else
			{
				popup.Detach();
			}
		}
		T typedValue = accessor.GetTypedValue();
		LocalizedString displayValue = popup.GetDisplayValue(typedValue);
		if (!object.Equals(typedValue, m_Value) || !m_DisplayValue.Equals(typedValue))
		{
			widgetChanges |= WidgetChanges.Properties;
			m_Value = typedValue;
			m_DisplayValue = displayValue;
		}
		if (expanded && popup.Update())
		{
			widgetChanges |= WidgetChanges.Children;
		}
		return widgetChanges;
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("expanded");
		writer.Write(m_LastExpanded);
		writer.PropertyName("displayValue");
		writer.Write(m_DisplayValue);
	}
}
