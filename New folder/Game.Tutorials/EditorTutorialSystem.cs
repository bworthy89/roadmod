using System.Collections.Generic;
using Colossal.Serialization.Entities;
using Game.Prefabs;
using Game.SceneFlow;
using Game.Settings;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Tutorials;

public class EditorTutorialSystem : TutorialSystem
{
	protected override Dictionary<string, bool> ShownTutorials => SharedSettings.instance.editor.shownTutorials;

	public override bool tutorialEnabled
	{
		get
		{
			return SharedSettings.instance.editor.showTutorials;
		}
		set
		{
			SharedSettings.instance.editor.showTutorials = value;
			if (!value)
			{
				base.mode = TutorialMode.Default;
			}
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Setting = SharedSettings.instance.editor;
		m_TutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<EditorTutorial>());
		m_ActiveTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<TutorialActive>(), ComponentType.ReadOnly<EditorTutorial>());
		m_PendingTutorialListQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialListData>(), ComponentType.ReadOnly<TutorialRef>(), ComponentType.ReadOnly<TutorialActivated>(), ComponentType.Exclude<TutorialCompleted>(), ComponentType.ReadOnly<EditorTutorial>());
		m_PendingTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<TutorialPhaseRef>(), ComponentType.ReadOnly<TutorialActivated>(), ComponentType.Exclude<TutorialActive>(), ComponentType.Exclude<TutorialCompleted>(), ComponentType.ReadOnly<EditorTutorial>());
		m_PendingPriorityTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<TutorialPhaseRef>(), ComponentType.ReadOnly<TutorialActivated>(), ComponentType.ReadOnly<ReplaceActiveData>(), ComponentType.Exclude<TutorialActive>(), ComponentType.Exclude<TutorialCompleted>(), ComponentType.ReadOnly<EditorTutorial>());
		m_LockedTutorialQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialData>(), ComponentType.ReadOnly<Locked>(), ComponentType.ReadOnly<EditorTutorial>());
	}

	public override void OnResetTutorials()
	{
		ShownTutorials.Clear();
		base.OnResetTutorials();
		if (GameManager.instance.gameMode.IsEditor())
		{
			m_Mode = TutorialMode.ListIntro;
		}
	}

	protected override void OnGamePreload(Purpose purpose, GameMode gameMode)
	{
		base.OnGamePreload(purpose, gameMode);
		base.Enabled = gameMode.IsEditor();
	}

	protected override void OnGameLoadingComplete(Purpose purpose, GameMode gameMode)
	{
		base.OnGameLoadingComplete(purpose, gameMode);
		if (gameMode == GameMode.Editor && tutorialEnabled && !ShownTutorials.ContainsKey(TutorialSystem.kListIntroKey))
		{
			m_Mode = TutorialMode.ListIntro;
		}
	}

	[Preserve]
	public EditorTutorialSystem()
	{
	}
}
