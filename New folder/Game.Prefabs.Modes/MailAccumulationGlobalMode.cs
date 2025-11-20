using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Global/", new Type[] { })]
public class MailAccumulationGlobalMode : EntityQueryModePrefab
{
	[BurstCompile]
	private struct ModeJob : IJobChunk
	{
		public float m_SendingRateMultiplier;

		public float m_ReceivingRateMultiplier;

		public ComponentTypeHandle<MailAccumulationData> m_MailAccumulateType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<MailAccumulationData> nativeArray = chunk.GetNativeArray(ref m_MailAccumulateType);
			for (int i = 0; i < chunk.Count; i++)
			{
				MailAccumulationData value = nativeArray[i];
				value.m_AccumulationRate = new float2(value.m_AccumulationRate.x * m_SendingRateMultiplier, value.m_AccumulationRate.y * m_ReceivingRateMultiplier);
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public float m_SendingRateMultiplier;

	public float m_ReceivingRateMultiplier;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<MailAccumulationData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<MailAccumulationData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		return JobChunkExtensions.ScheduleParallel(new ModeJob
		{
			m_SendingRateMultiplier = m_SendingRateMultiplier,
			m_ReceivingRateMultiplier = m_ReceivingRateMultiplier,
			m_MailAccumulateType = entityManager.GetComponentTypeHandle<MailAccumulationData>(isReadOnly: false)
		}, requestedQuery, deps);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			if (!prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefabBase) || !prefabBase.TryGetExactly<MailAccumulation>(out var component))
			{
				ComponentBase.baseLog.Warn($"Prefab data not found {this} : {entity.ToString()} : {prefabBase}");
				continue;
			}
			MailAccumulationData componentData = entityManager.GetComponentData<MailAccumulationData>(entity);
			componentData.m_AccumulationRate = new float2(component.m_SendingRate, component.m_ReceivingRate);
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
