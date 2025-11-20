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
public class ZonePollutionGlobalMode : EntityQueryModePrefab
{
	[BurstCompile]
	private struct ModeJob : IJobChunk
	{
		public float m_GroundPollutionMultiplier;

		public float m_AirPollutionMultiplier;

		public float m_NoisePollutionMultiplier;

		public ComponentTypeHandle<PollutionData> m_PollutionType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<PollutionData> nativeArray = chunk.GetNativeArray(ref m_PollutionType);
			for (int i = 0; i < chunk.Count; i++)
			{
				PollutionData value = nativeArray[i];
				value.m_GroundPollution *= m_GroundPollutionMultiplier;
				value.m_AirPollution *= m_AirPollutionMultiplier;
				value.m_NoisePollution *= m_NoisePollutionMultiplier;
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[Header("Modify the pollution of zone buildings, excluding others.")]
	public float m_GroundPollutionMultiplier;

	public float m_AirPollutionMultiplier;

	public float m_NoisePollutionMultiplier;

	private Dictionary<Entity, PollutionData> m_OriginalPollutionData;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[3]
		{
			ComponentType.ReadOnly<SpawnableBuildingData>(),
			ComponentType.ReadOnly<BuildingSpawnGroupData>(),
			ComponentType.ReadOnly<PollutionData>()
		};
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<PollutionData>(entity);
	}

	public override void StoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		m_OriginalPollutionData = new Dictionary<Entity, PollutionData>(entities.Length);
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			PollutionData componentData = entityManager.GetComponentData<PollutionData>(entity);
			m_OriginalPollutionData.Add(entity, componentData);
		}
		entities.Dispose();
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		return JobChunkExtensions.ScheduleParallel(new ModeJob
		{
			m_GroundPollutionMultiplier = m_GroundPollutionMultiplier,
			m_AirPollutionMultiplier = m_AirPollutionMultiplier,
			m_NoisePollutionMultiplier = m_NoisePollutionMultiplier,
			m_PollutionType = entityManager.GetComponentTypeHandle<PollutionData>(isReadOnly: false)
		}, requestedQuery, deps);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			if (m_OriginalPollutionData.TryGetValue(entity, out var value))
			{
				entityManager.SetComponentData(entity, value);
			}
			else
			{
				m_OriginalPollutionData.Add(entity, entityManager.GetComponentData<PollutionData>(entity));
			}
		}
	}
}
