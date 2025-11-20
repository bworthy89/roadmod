using Game.Common;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Tutorials;

public abstract class TutorialDeactivationSystemBase : GameSystemBase
{
	private EntityQuery m_ActivePhaseQuery;

	protected EntityCommandBufferSystem m_BarrierSystem;

	protected bool phaseCanDeactivate => !m_ActivePhaseQuery.IsEmptyIgnoreFilter;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_BarrierSystem = base.World.GetOrCreateSystemManaged<ModificationBarrier3>();
		m_ActivePhaseQuery = GetEntityQuery(ComponentType.ReadOnly<TutorialPhaseData>(), ComponentType.ReadOnly<TutorialPhaseActive>(), ComponentType.ReadOnly<TutorialPhaseCanDeactivate>());
	}

	[Preserve]
	protected TutorialDeactivationSystemBase()
	{
	}
}
