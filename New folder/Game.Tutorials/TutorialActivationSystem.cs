using System;
using System.Collections.Generic;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using UnityEngine.Scripting;

namespace Game.Tutorials;

public class TutorialActivationSystem : GameSystemBase
{
	private readonly List<GameSystemBase> m_Systems = new List<GameSystemBase>();

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialUIActivationSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialAutoActivationSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialControlSchemeActivationSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialObjectSelectedActivationSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialInfoviewActivationSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialFireActivationSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialHealthProblemActivationSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialEventActivationSystem>());
		base.Enabled = false;
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		base.Enabled = mode.IsGame() || mode.IsEditor();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		foreach (GameSystemBase system in m_Systems)
		{
			try
			{
				system.Update();
			}
			catch (Exception exception)
			{
				COSystemBase.baseLog.Critical(exception);
			}
		}
	}

	[Preserve]
	public TutorialActivationSystem()
	{
	}
}
