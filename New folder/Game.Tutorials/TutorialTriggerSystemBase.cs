using Colossal.Serialization.Entities;
using Game.Common;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Tutorials;

public abstract class TutorialTriggerSystemBase : GameSystemBase
{
	protected ModificationBarrier5 m_BarrierSystem;

	protected EntityQuery m_ActiveTriggerQuery;

	private TutorialSystem m_TutorialSystem;

	private Entity m_LastPhase;

	protected bool triggersChanged { get; private set; }

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BarrierSystem = base.World.GetOrCreateSystemManaged<ModificationBarrier5>();
		m_TutorialSystem = base.World.GetOrCreateSystemManaged<TutorialSystem>();
	}

	protected override void OnGameLoaded(Context serializationContext)
	{
		base.OnGameLoaded(serializationContext);
		m_LastPhase = Entity.Null;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		Entity activeTutorialPhase = m_TutorialSystem.activeTutorialPhase;
		if (activeTutorialPhase != m_LastPhase)
		{
			m_LastPhase = activeTutorialPhase;
			triggersChanged = true;
		}
		else
		{
			triggersChanged = false;
		}
	}

	[Preserve]
	protected override void OnStopRunning()
	{
		base.OnStopRunning();
		m_LastPhase = Entity.Null;
	}

	[Preserve]
	protected TutorialTriggerSystemBase()
	{
	}
}
