using System;
using Game.UI.Widgets;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

public class BulldozeToolPanel : EditorPanelSystemBase
{
	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		children = Array.Empty<IWidget>();
		title = "Editor.TOOL[BulldozeTool]";
	}

	[Preserve]
	public BulldozeToolPanel()
	{
	}
}
