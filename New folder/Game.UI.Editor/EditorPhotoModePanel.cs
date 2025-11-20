using Game.UI.InGame;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

public class EditorPhotoModePanel : EditorPanelSystemBase
{
	private PhotoModeUISystem m_PhotoModeUISystem;

	public override EditorPanelWidgetRenderer widgetRenderer => EditorPanelWidgetRenderer.PhotoMode;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		title = "PhotoMode.TITLE";
		m_PhotoModeUISystem = base.World.GetOrCreateSystemManaged<PhotoModeUISystem>();
	}

	[Preserve]
	protected override void OnStartRunning()
	{
		base.OnStartRunning();
		m_PhotoModeUISystem.Activate(enabled: true);
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		base.OnStopRunning();
		m_PhotoModeUISystem.Activate(enabled: false);
	}

	[Preserve]
	public EditorPhotoModePanel()
	{
	}
}
