using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Global/", new Type[] { })]
public class ElectricityConnectionGlobalMode : EntityQueryModePrefab
{
	[BurstCompile]
	private struct ModeJob : IJobChunk
	{
		public float m_CapacityMultiplier;

		public ComponentTypeHandle<ElectricityConnectionData> m_ElectricityConnectionType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<ElectricityConnectionData> nativeArray = chunk.GetNativeArray(ref m_ElectricityConnectionType);
			for (int i = 0; i < chunk.Count; i++)
			{
				ElectricityConnectionData value = nativeArray[i];
				value.m_Capacity = (int)((float)value.m_Capacity * m_CapacityMultiplier);
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public float m_CapacityMultiplier;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<ElectricityConnectionData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<ElectricityConnectionData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		return JobChunkExtensions.ScheduleParallel(new ModeJob
		{
			m_ElectricityConnectionType = entityManager.GetComponentTypeHandle<ElectricityConnectionData>(isReadOnly: false),
			m_CapacityMultiplier = m_CapacityMultiplier
		}, requestedQuery, deps);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			if (!prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefabBase) || !prefabBase.TryGetExactly<ElectricityConnection>(out var component))
			{
				ComponentBase.baseLog.Warn($"Prefab data not found {this} : {entity.ToString()} : {prefabBase}");
				continue;
			}
			ElectricityConnectionData componentData = entityManager.GetComponentData<ElectricityConnectionData>(entity);
			componentData.m_Capacity = component.m_Capacity;
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
