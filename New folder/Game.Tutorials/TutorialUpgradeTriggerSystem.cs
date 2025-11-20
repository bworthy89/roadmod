using Game.Buildings;
using Game.Common;
using Game.Prefabs;
using Game.Tools;
using Unity.Collections;
using Unity.Entities;
using UnityEngine.Scripting;

namespace Game.Tutorials;

public class TutorialUpgradeTriggerSystem : TutorialTriggerSystemBase
{
	private EntityQuery m_CreatedUpgradeQuery;

	private EntityQuery m_UpgradeQuery;

	private EntityArchetype m_UnlockEventArchetype;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CreatedUpgradeQuery = GetEntityQuery(ComponentType.ReadOnly<Created>(), ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Native>());
		m_UpgradeQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Native>());
		m_ActiveTriggerQuery = GetEntityQuery(ComponentType.ReadOnly<UpgradeTriggerData>(), ComponentType.ReadOnly<TriggerActive>(), ComponentType.Exclude<TriggerCompleted>());
		m_UnlockEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Event>(), ComponentType.ReadWrite<Unlock>());
		RequireForUpdate(m_ActiveTriggerQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		base.OnUpdate();
		if (base.triggersChanged && !m_UpgradeQuery.IsEmptyIgnoreFilter)
		{
			EntityCommandBuffer commandBuffer = m_BarrierSystem.CreateCommandBuffer();
			NativeArray<Entity> nativeArray = m_ActiveTriggerQuery.ToEntityArray(Allocator.TempJob);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				commandBuffer.AddComponent<TriggerPreCompleted>(nativeArray[i]);
				TutorialSystem.ManualUnlock(nativeArray[i], m_UnlockEventArchetype, base.EntityManager, commandBuffer);
			}
			nativeArray.Dispose();
		}
		if (!m_CreatedUpgradeQuery.IsEmptyIgnoreFilter)
		{
			EntityCommandBuffer commandBuffer2 = m_BarrierSystem.CreateCommandBuffer();
			NativeArray<Entity> nativeArray2 = m_ActiveTriggerQuery.ToEntityArray(Allocator.TempJob);
			for (int j = 0; j < nativeArray2.Length; j++)
			{
				commandBuffer2.AddComponent<TriggerCompleted>(nativeArray2[j]);
				TutorialSystem.ManualUnlock(nativeArray2[j], m_UnlockEventArchetype, base.EntityManager, commandBuffer2);
			}
			nativeArray2.Dispose();
		}
	}

	[Preserve]
	public TutorialUpgradeTriggerSystem()
	{
	}
}
