using Colossal.UI.Binding;
using UnityEngine.Scripting;

namespace Game.UI.Editor;

public class EditorScreenUISystem : UISystemBase
{
	public enum EditorScreen
	{
		Main,
		PauseMenu,
		Options,
		FreeCamera
	}

	private const string kGroup = "editor";

	private ValueBinding<EditorScreen> m_ActiveScreenBinding;

	public EditorScreen activeScreen
	{
		get
		{
			return m_ActiveScreenBinding.value;
		}
		set
		{
			m_ActiveScreenBinding.Update(value);
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		AddBinding(m_ActiveScreenBinding = new ValueBinding<EditorScreen>("editor", "activeScreen", EditorScreen.Main, new EnumWriter<EditorScreen>()));
		AddBinding(new TriggerBinding<EditorScreen>("editor", "setActiveScreen", SetScreen, new EnumReader<EditorScreen>()));
	}

	public void SetScreen(EditorScreen screen)
	{
		m_ActiveScreenBinding.Update(screen);
	}

	[Preserve]
	public EditorScreenUISystem()
	{
	}
}
