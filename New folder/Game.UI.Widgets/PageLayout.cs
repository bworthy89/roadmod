using System;
using Colossal.Annotations;
using Colossal.UI.Binding;
using Game.UI.Localization;

namespace Game.UI.Widgets;

internal class PageLayout : LayoutContainer, IInvokable, IWidget, IJsonWritable
{
	public LocalizedString title { get; set; }

	[CanBeNull]
	public Action backAction { get; set; }

	public PageLayout()
	{
		base.flex = FlexLayout.Fill;
	}

	public void Invoke()
	{
		backAction();
	}

	protected override void WriteProperties(IJsonWriter writer)
	{
		base.WriteProperties(writer);
		writer.PropertyName("title");
		writer.Write(title);
		writer.PropertyName("hasBackAction");
		writer.Write(backAction != null);
	}
}
