using System;
using System.Collections.Generic;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public class Group : NamedWidgetWithTooltip, IContainerWidget
{
	public enum TooltipPosition
	{
		Title,
		Container
	}

	private IList<IWidget> m_Children = Array.Empty<IWidget>();

	public TooltipPosition tooltipPos { get; set; } = TooltipPosition.Container;

	public IList<IWidget> children
	{
		get
		{
			return m_Children;
		}
		set
		{
			if (value != m_Children)
			{
				ContainerExtensions.SetDefaults(value);
				m_Children = value;
				SetChildrenChanged();
			}
		}
	}

	public override IList<IWidget> visibleChildren => children;

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("tooltipPos");
		writer.Write((int)tooltipPos);
	}
}
