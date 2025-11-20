using System.Runtime.CompilerServices;
using Game.Common;
using Game.Simulation;
using Game.UI.InGame;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class UnlockAllSystem : GameSystemBase
{
	private MilestoneSystem m_MilestoneSystem;

	private ModificationBarrier1 m_ModificationBarrier;

	private UIHighlightSystem m_UIHighlightSystem;

	private SignatureBuildingUISystem m_SignatureBuildingUISystem;

	private EntityQuery m_LockedQuery;

	private EntityArchetype m_UnlockEventArchetype;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_MilestoneSystem = base.World.GetOrCreateSystemManaged<MilestoneSystem>();
		m_ModificationBarrier = base.World.GetOrCreateSystemManaged<ModificationBarrier1>();
		m_UIHighlightSystem = base.World.GetOrCreateSystemManaged<UIHighlightSystem>();
		m_SignatureBuildingUISystem = base.World.GetOrCreateSystemManaged<SignatureBuildingUISystem>();
		m_LockedQuery = GetEntityQuery(ComponentType.ReadOnly<Locked>(), ComponentType.Exclude<MilestoneData>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Unlock>());
		RequireForUpdate(m_LockedQuery);
		base.Enabled = false;
	}

	[Preserve]
	protected override void OnUpdate()
	{
		UnlockAllImpl();
		base.Enabled = false;
	}

	private void UnlockAllImpl()
	{
		EntityCommandBuffer entityCommandBuffer = m_ModificationBarrier.CreateCommandBuffer();
		NativeArray<Entity> nativeArray = m_LockedQuery.ToEntityArray(Allocator.TempJob);
		for (int i = 0; i < nativeArray.Length; i++)
		{
			Entity prefab = nativeArray[i];
			Entity e = entityCommandBuffer.CreateEntity(m_UnlockEventArchetype);
			entityCommandBuffer.SetComponent(e, new Unlock(prefab));
		}
		nativeArray.Dispose();
		m_MilestoneSystem.UnlockAllMilestones();
		m_UIHighlightSystem.SkipUpdate();
		m_SignatureBuildingUISystem.SkipUpdate();
	}

	[Preserve]
	public UnlockAllSystem()
	{
	}
}
