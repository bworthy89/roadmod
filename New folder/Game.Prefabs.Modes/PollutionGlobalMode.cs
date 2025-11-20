using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Global/", new Type[] { })]
public class PollutionGlobalMode : EntityQueryModePrefab
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

	[Header("Modify the pollution of buildings, excluding zone buildings.")]
	public float m_GroundPollutionMultiplier;

	public float m_AirPollutionMultiplier;

	public float m_NoisePollutionMultiplier;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<PollutionData>() };
		entityQueryDesc.Any = new ComponentType[2]
		{
			ComponentType.ReadOnly<BuildingData>(),
			ComponentType.ReadOnly<BuildingExtensionData>()
		};
		entityQueryDesc.None = new ComponentType[1] { ComponentType.ReadOnly<SpawnableBuildingData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<PollutionData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		return JobChunkExtensions.ScheduleParallel(new ModeJob
		{
			m_GroundPollutionMultiplier = m_GroundPollutionMultiplier,
			m_NoisePollutionMultiplier = m_NoisePollutionMultiplier,
			m_AirPollutionMultiplier = m_AirPollutionMultiplier,
			m_PollutionType = entityManager.GetComponentTypeHandle<PollutionData>(isReadOnly: false)
		}, requestedQuery, deps);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			if (!prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefabBase) || !prefabBase.TryGetExactly<Pollution>(out var component))
			{
				ComponentBase.baseLog.Warn($"Prefab data not found {this} : {entity.ToString()} : {prefabBase}");
				continue;
			}
			PollutionData componentData = entityManager.GetComponentData<PollutionData>(entity);
			componentData.m_GroundPollution = component.m_GroundPollution;
			componentData.m_AirPollution = component.m_AirPollution;
			componentData.m_NoisePollution = component.m_NoisePollution;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
