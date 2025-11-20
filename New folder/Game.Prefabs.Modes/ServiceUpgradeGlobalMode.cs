using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Global/", new Type[] { })]
public class ServiceUpgradeGlobalMode : EntityQueryModePrefab
{
	[BurstCompile]
	private struct ModeJob : IJobChunk
	{
		public float m_UpgradeCostMultiplier;

		public float m_XPRewardMultiplier;

		public ComponentTypeHandle<ServiceUpgradeData> m_ServiceUpgradeType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<ServiceUpgradeData> nativeArray = chunk.GetNativeArray(ref m_ServiceUpgradeType);
			for (int i = 0; i < chunk.Count; i++)
			{
				ServiceUpgradeData value = nativeArray[i];
				value.m_UpgradeCost = (uint)((float)value.m_UpgradeCost * m_UpgradeCostMultiplier);
				value.m_XPReward = (int)((float)value.m_XPReward * m_XPRewardMultiplier);
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public float m_UpgradeCostMultiplier;

	public float m_XPRewardMultiplier;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[2]
		{
			ComponentType.ReadOnly<ServiceUpgradeData>(),
			ComponentType.ReadOnly<ServiceUpgradeBuilding>()
		};
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<ServiceUpgradeData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		return JobChunkExtensions.ScheduleParallel(new ModeJob
		{
			m_UpgradeCostMultiplier = m_UpgradeCostMultiplier,
			m_XPRewardMultiplier = m_XPRewardMultiplier,
			m_ServiceUpgradeType = entityManager.GetComponentTypeHandle<ServiceUpgradeData>(isReadOnly: false)
		}, requestedQuery, deps);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			if (!prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefabBase) || !prefabBase.TryGetExactly<ServiceUpgrade>(out var component))
			{
				ComponentBase.baseLog.Warn($"Prefab data not found {this} : {entity.ToString()} : {prefabBase}");
				continue;
			}
			ServiceUpgradeData componentData = entityManager.GetComponentData<ServiceUpgradeData>(entity);
			componentData.m_UpgradeCost = component.m_UpgradeCost;
			componentData.m_XPReward = component.m_XPReward;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
