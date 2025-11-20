using System.Collections.Generic;
using Colossal.Annotations;
using Game.UI.Localization;
using Game.UI.Widgets;

namespace Game.UI.Editor;

public interface IEditorPanel
{
	[CanBeNull]
	LocalizedString title { get; }

	IList<IWidget> children { get; }

	EditorPanelWidgetRenderer widgetRenderer { get; }

	void OnValueChanged(IWidget widget);

	bool OnCancel();

	bool OnClose();
}
