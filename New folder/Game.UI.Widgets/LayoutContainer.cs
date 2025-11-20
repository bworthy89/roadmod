using System;
using System.Collections.Generic;
using Colossal.UI.Binding;

namespace Game.UI.Widgets;

public abstract class LayoutContainer : Widget, IContainerWidget
{
	private IList<IWidget> m_Children = Array.Empty<IWidget>();

	public FlexLayout flex { get; set; } = FlexLayout.Default;

	public IList<IWidget> children
	{
		get
		{
			return m_Children;
		}
		set
		{
			if (!object.Equals(value, m_Children))
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
		writer.PropertyName("flex");
		writer.Write(flex);
	}
}
