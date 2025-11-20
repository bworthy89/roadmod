using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Global/", new Type[] { })]
public class ServiceCoverageGlobalMode : EntityQueryModePrefab
{
	[BurstCompile]
	private struct ModeJob : IJobChunk
	{
		public float m_RangeMultiplier;

		public float m_CapacityMultiplier;

		public float m_MagnitudeMultiplier;

		public ComponentTypeHandle<CoverageData> m_CoverageType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<CoverageData> nativeArray = chunk.GetNativeArray(ref m_CoverageType);
			for (int i = 0; i < chunk.Count; i++)
			{
				CoverageData value = nativeArray[i];
				value.m_Range *= m_RangeMultiplier;
				value.m_Capacity *= m_CapacityMultiplier;
				value.m_Magnitude *= m_MagnitudeMultiplier;
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public float m_RangeMultiplier;

	public float m_CapacityMultiplier;

	public float m_MagnitudeMultiplier;

	private Dictionary<Entity, CoverageData> m_OriginalCoverageData;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<CoverageData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<CoverageData>(entity);
	}

	public override void StoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		m_OriginalCoverageData = new Dictionary<Entity, CoverageData>();
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			CoverageData componentData = entityManager.GetComponentData<CoverageData>(entity);
			if (prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefabBase) && prefabBase.TryGetExactly<ServiceCoverage>(out var _))
			{
				m_OriginalCoverageData[entity] = componentData;
			}
		}
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		return JobChunkExtensions.ScheduleParallel(new ModeJob
		{
			m_RangeMultiplier = m_RangeMultiplier,
			m_MagnitudeMultiplier = m_MagnitudeMultiplier,
			m_CapacityMultiplier = m_CapacityMultiplier,
			m_CoverageType = entityManager.GetComponentTypeHandle<CoverageData>(isReadOnly: false)
		}, requestedQuery, deps);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			PrefabBase prefabBase;
			ServiceCoverage component;
			if (m_OriginalCoverageData.TryGetValue(entity, out var value))
			{
				entityManager.SetComponentData(entity, value);
			}
			else if (prefabSystem.TryGetPrefab<PrefabBase>(entity, out prefabBase) && prefabBase.TryGetExactly<ServiceCoverage>(out component))
			{
				CoverageData componentData = entityManager.GetComponentData<CoverageData>(entity);
				componentData.m_Range = component.m_Range;
				componentData.m_Capacity = component.m_Capacity;
				componentData.m_Magnitude = component.m_Magnitude;
				entityManager.SetComponentData(entity, componentData);
			}
			else
			{
				m_OriginalCoverageData.Add(entity, entityManager.GetComponentData<CoverageData>(entity));
			}
		}
	}
}
