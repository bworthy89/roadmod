using System;
using System.Collections.Generic;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using UnityEngine.Scripting;

namespace Game.Tutorials;

public class TutorialDeactivationSystem : GameSystemBase
{
	private List<TutorialDeactivationSystemBase> m_Systems = new List<TutorialDeactivationSystemBase>();

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialControlSchemeDeactivationSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialUIDeactivationSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialObjectSelectionDeactivationSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialInfoviewDeactivationSystem>());
		base.Enabled = false;
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		base.Enabled = mode.IsGame();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		foreach (TutorialDeactivationSystemBase system in m_Systems)
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
	public TutorialDeactivationSystem()
	{
	}
}
