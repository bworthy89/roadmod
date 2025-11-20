using System;
using System.Collections.Generic;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Serialization;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Tutorials;

public class TutorialTriggerSystem : GameSystemBase, IPreDeserialize
{
	private readonly List<TutorialTriggerSystemBase> m_Systems = new List<TutorialTriggerSystemBase>();

	private EntityQuery m_TriggerQuery;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialObjectPlacementTriggerSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialInputTriggerSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialAreaTriggerSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialObjectSelectionTriggerSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialUpgradeTriggerSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialUITriggerSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialPolicyAdjustmentTriggerSystem>());
		m_Systems.Add(base.World.GetOrCreateSystemManaged<TutorialZoningTriggerSystem>());
		m_TriggerQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialTriggerData>());
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
		foreach (TutorialTriggerSystemBase system in m_Systems)
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

	public void PreDeserialize(Context context)
	{
		base.EntityManager.RemoveComponent<TriggerActive>(m_TriggerQuery);
		base.EntityManager.RemoveComponent<TriggerPreCompleted>(m_TriggerQuery);
		base.EntityManager.RemoveComponent<TriggerCompleted>(m_TriggerQuery);
		base.EntityManager.RemoveComponent<TutorialNextPhase>(m_TriggerQuery);
	}

	[Preserve]
	public TutorialTriggerSystem()
	{
	}
}
