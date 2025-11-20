using System;
using System.Collections.Generic;
using Game.UI.Localization;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public abstract class EditorPanelBase : IEditorPanel
{
	public LocalizedString title { get; set; }

	public IList<IWidget> children { get; set; } = Array.Empty<IWidget>();

	public virtual EditorPanelWidgetRenderer widgetRenderer => EditorPanelWidgetRenderer.Editor;

	public virtual void OnValueChanged(IWidget widget)
	{
	}

	public virtual bool OnCancel()
	{
		return OnClose();
	}

	public virtual bool OnClose()
	{
		return true;
	}
}
