using System;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Global/", new Type[] { })]
public class NetPollutionGlobalMode : EntityQueryModePrefab
{
	[BurstCompile]
	private struct ModeJob : IJobChunk
	{
		public float m_NoisePollutionFactorMultiplier;

		public float m_AirPollutionFactorMultiplier;

		public ComponentTypeHandle<NetPollutionData> m_NetPollutionType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<NetPollutionData> nativeArray = chunk.GetNativeArray(ref m_NetPollutionType);
			for (int i = 0; i < chunk.Count; i++)
			{
				NetPollutionData value = nativeArray[i];
				value.m_Factors = new float2(value.m_Factors.x * m_NoisePollutionFactorMultiplier, value.m_Factors.y * m_AirPollutionFactorMultiplier);
				nativeArray[i] = value;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	public float m_NoisePollutionFactorMultiplier;

	public float m_AirPollutionFactorMultiplier;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<NetPollutionData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<NetPollutionData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		return JobChunkExtensions.ScheduleParallel(new ModeJob
		{
			m_NoisePollutionFactorMultiplier = m_NoisePollutionFactorMultiplier,
			m_AirPollutionFactorMultiplier = m_AirPollutionFactorMultiplier,
			m_NetPollutionType = entityManager.GetComponentTypeHandle<NetPollutionData>(isReadOnly: false)
		}, requestedQuery, deps);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			if (!prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefabBase) || !prefabBase.TryGetExactly<NetPollution>(out var component))
			{
				ComponentBase.baseLog.Warn($"Prefab data not found {this} : {entity.ToString()} : {prefabBase}");
				continue;
			}
			NetPollutionData componentData = entityManager.GetComponentData<NetPollutionData>(entity);
			componentData.m_Factors = new float2(component.m_NoisePollutionFactor, component.m_AirPollutionFactor);
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
