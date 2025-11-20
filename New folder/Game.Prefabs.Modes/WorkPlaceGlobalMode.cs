using System;
using System.Collections.Generic;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Global/", new Type[] { })]
public class WorkPlaceGlobalMode : EntityQueryModePrefab
{
	[BurstCompile]
	private struct ModeJob : IJobChunk
	{
		public float m_WorkplacesMultiplier;

		public ComponentTypeHandle<WorkplaceData> m_WorkplaceType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<WorkplaceData> nativeArray = chunk.GetNativeArray(ref m_WorkplaceType);
			for (int i = 0; i < chunk.Count; i++)
			{
				WorkplaceData value = nativeArray[i];
				value.m_MaxWorkers = (int)((float)value.m_MaxWorkers * m_WorkplacesMultiplier);
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public float m_WorkplacesMultiplier;

	private Dictionary<Entity, WorkplaceData> m_OriginalWorkplaceData;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[2]
		{
			ComponentType.ReadOnly<WorkplaceData>(),
			ComponentType.ReadOnly<BuildingData>()
		};
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<WorkplaceData>(entity);
	}

	public override void StoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		m_OriginalWorkplaceData = new Dictionary<Entity, WorkplaceData>();
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			WorkplaceData componentData = entityManager.GetComponentData<WorkplaceData>(entity);
			if (prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefabBase) && prefabBase.TryGetExactly<Workplace>(out var _))
			{
				m_OriginalWorkplaceData[entity] = componentData;
			}
		}
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		return JobChunkExtensions.ScheduleParallel(new ModeJob
		{
			m_WorkplacesMultiplier = m_WorkplacesMultiplier,
			m_WorkplaceType = entityManager.GetComponentTypeHandle<WorkplaceData>(isReadOnly: false)
		}, requestedQuery, deps);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			WorkplaceData componentData = entityManager.GetComponentData<WorkplaceData>(entity);
			PrefabBase prefabBase;
			Workplace component;
			if (m_OriginalWorkplaceData.TryGetValue(entity, out var value))
			{
				componentData.m_MaxWorkers = value.m_MaxWorkers;
			}
			else if (prefabSystem.TryGetPrefab<PrefabBase>(entity, out prefabBase) && prefabBase.TryGetExactly<Workplace>(out component))
			{
				componentData.m_MaxWorkers = component.m_Workplaces;
			}
			else
			{
				m_OriginalWorkplaceData.Add(entity, entityManager.GetComponentData<WorkplaceData>(entity));
			}
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
