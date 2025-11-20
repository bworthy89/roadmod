using System;
using System.Collections.Generic;
using Game.Economy;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Global/", new Type[] { })]
public class ServiceConsumptionGlobalMode : EntityQueryModePrefab
{
	[BurstCompile]
	private struct ModeJob : IJobChunk
	{
		public float m_UpkeepMultiplier;

		public float m_ElectricityConsumptionMultiplier;

		public float m_WaterConsumptionMultiplier;

		public float m_GarbageAccumlationMultiplier;

		public ComponentTypeHandle<ConsumptionData> m_ConsumptionType;

		public BufferTypeHandle<ServiceUpkeepData> m_ServiceUpkeepType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<ConsumptionData> nativeArray = chunk.GetNativeArray(ref m_ConsumptionType);
			BufferAccessor<ServiceUpkeepData> bufferAccessor = chunk.GetBufferAccessor(ref m_ServiceUpkeepType);
			for (int i = 0; i < chunk.Count; i++)
			{
				ConsumptionData value = nativeArray[i];
				value.m_Upkeep = (int)((float)value.m_Upkeep * m_UpkeepMultiplier);
				value.m_ElectricityConsumption = (int)(value.m_ElectricityConsumption * m_ElectricityConsumptionMultiplier);
				value.m_WaterConsumption = (int)(value.m_WaterConsumption * m_WaterConsumptionMultiplier);
				value.m_GarbageAccumulation = (int)(value.m_GarbageAccumulation * m_GarbageAccumlationMultiplier);
				nativeArray[i] = value;
				DynamicBuffer<ServiceUpkeepData> dynamicBuffer = bufferAccessor[i];
				for (int j = 0; j < dynamicBuffer.Length; j++)
				{
					if (dynamicBuffer[j].m_Upkeep.m_Resource == Resource.Money)
					{
						ServiceUpkeepData value2 = dynamicBuffer[j];
						value2.m_Upkeep.m_Amount = (int)((float)value2.m_Upkeep.m_Amount * m_UpkeepMultiplier);
						dynamicBuffer[j] = value2;
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[Header("Modify the upkeep and consumption of service buildings, excluding zone buildings.")]
	public float m_UpkeepMultiplier;

	public float m_ElectricityConsumptionMultiplier;

	public float m_WaterConsumptionMultiplier;

	public float m_GarbageAccumlationMultiplier;

	private Dictionary<Entity, ServiceUpkeepData> m_CachedUpkeepDatasDatas;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[2]
		{
			ComponentType.ReadOnly<ConsumptionData>(),
			ComponentType.ReadOnly<ServiceUpkeepData>()
		};
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
		entityManager.GetBuffer<ServiceUpkeepData>(entity);
		entityManager.GetComponentData<ConsumptionData>(entity);
	}

	public override void StoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		m_CachedUpkeepDatasDatas = new Dictionary<Entity, ServiceUpkeepData>(entities.Length);
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			DynamicBuffer<ServiceUpkeepData> buffer = entityManager.GetBuffer<ServiceUpkeepData>(entity);
			for (int j = 0; j < buffer.Length; j++)
			{
				if (buffer[j].m_Upkeep.m_Resource == Resource.Money)
				{
					m_CachedUpkeepDatasDatas.Add(entity, buffer[j]);
				}
			}
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
			m_ConsumptionType = entityManager.GetComponentTypeHandle<ConsumptionData>(isReadOnly: false),
			m_ServiceUpkeepType = entityManager.GetBufferTypeHandle<ServiceUpkeepData>(isReadOnly: false)
		}, requestedQuery, deps);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < entities.Length; i++)
		{
			Entity entity = entities[i];
			if (!prefabSystem.TryGetPrefab<PrefabBase>(entity, out var prefabBase) || !prefabBase.TryGetExactly<ServiceConsumption>(out var component))
			{
				ComponentBase.baseLog.Warn($"Prefab data not found {this} : {entity.ToString()} : {prefabBase}");
				continue;
			}
			ConsumptionData componentData = entityManager.GetComponentData<ConsumptionData>(entity);
			componentData.m_Upkeep = component.m_Upkeep;
			componentData.m_ElectricityConsumption = component.m_ElectricityConsumption;
			componentData.m_WaterConsumption = component.m_WaterConsumption;
			componentData.m_GarbageAccumulation = component.m_GarbageAccumulation;
			entityManager.SetComponentData(entity, componentData);
			DynamicBuffer<ServiceUpkeepData> buffer = entityManager.GetBuffer<ServiceUpkeepData>(entity);
			for (int j = 0; j < buffer.Length; j++)
			{
				if (buffer[j].m_Upkeep.m_Resource == Resource.Money)
				{
					if (!m_CachedUpkeepDatasDatas.TryGetValue(entity, out var value))
					{
						ComponentBase.baseLog.Critical("Cached ServiceUpkeepData not found " + entity.ToString());
						continue;
					}
					ServiceUpkeepData value2 = buffer[j];
					value2.m_Upkeep.m_Amount = value.m_Upkeep.m_Amount;
					buffer[j] = value2;
				}
			}
		}
	}
}
