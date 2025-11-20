using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Global/", new Type[] { })]
public class ZoneServiceConsumptionGlobalMode : EntityQueryModePrefab
{
	[BurstCompile]
	private struct ModeJob : IJobChunk
	{
		public float m_UpkeepMultiplier;

		public float m_ElectricityConsumptionMultiplier;

		public float m_WaterConsumptionMultiplier;

		public float m_GarbageAccumlationMultiplier;

		public float m_TelecomNeedMultiplier;

		public ComponentTypeHandle<ConsumptionData> m_ConsumptionType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<ConsumptionData> nativeArray = chunk.GetNativeArray(ref m_ConsumptionType);
			for (int i = 0; i < chunk.Count; i++)
			{
				ConsumptionData value = nativeArray[i];
				value.m_Upkeep = (int)((float)value.m_Upkeep * m_UpkeepMultiplier);
				value.m_ElectricityConsumption *= m_ElectricityConsumptionMultiplier;
				value.m_WaterConsumption *= m_WaterConsumptionMultiplier;
				value.m_GarbageAccumulation *= m_GarbageAccumlationMultiplier;
				value.m_TelecomNeed *= m_TelecomNeedMultiplier;
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[Header("Modify the upkeep and consumption of zone buildings.")]
	public float m_UpkeepMultiplier;

	public float m_ElectricityConsumptionMultiplier;

	public float m_WaterConsumptionMultiplier;

	public float m_GarbageAccumlationMultiplier;

	public float m_TelecomNeedMultiplier;

	private Dictionary<Entity, ConsumptionData> m_OriginalConsumptionData;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[2]
		{
			ComponentType.ReadOnly<ConsumptionData>(),
			ComponentType.ReadOnly<SpawnableBuildingData>()
		};
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<ConsumptionData>(entity);
	}

	public override void StoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		m_OriginalConsumptionData = new Dictionary<Entity, ConsumptionData>(entities.Length);
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			ConsumptionData componentData = entityManager.GetComponentData<ConsumptionData>(entity);
			m_OriginalConsumptionData.Add(entity, componentData);
		}
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		return JobChunkExtensions.ScheduleParallel(new ModeJob
		{
			m_UpkeepMultiplier = m_UpkeepMultiplier,
			m_ElectricityConsumptionMultiplier = m_ElectricityConsumptionMultiplier,
			m_WaterConsumptionMultiplier = m_WaterConsumptionMultiplier,
			m_GarbageAccumlationMultiplier = m_GarbageAccumlationMultiplier,
			m_TelecomNeedMultiplier = m_TelecomNeedMultiplier,
			m_ConsumptionType = entityManager.GetComponentTypeHandle<ConsumptionData>(isReadOnly: false)
		}, requestedQuery, deps);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			if (m_OriginalConsumptionData.TryGetValue(entity, out var value))
			{
				entityManager.SetComponentData(entity, value);
				continue;
			}
			value = entityManager.GetComponentData<ConsumptionData>(entity);
			m_OriginalConsumptionData.Add(entity, value);
		}
	}
}
