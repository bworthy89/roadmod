using Colossal.UI.Binding;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Prefabs;
using Game.Simulation;
using Game.Tools;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.UI.InGame;

public class PostInfoviewUISystem : InfoviewUISystemBase
{
	[BurstCompile]
	private struct UpdateMailRateJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefHandle;

		[ReadOnly]
		public ComponentLookup<ServiceObjectData> m_ServiceObjectFromEntity;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableDataFromEntity;

		[ReadOnly]
		public ComponentLookup<MailAccumulationData> m_MailAccumulationFromEntity;

		[ReadOnly]
		public BufferLookup<Renter> m_RenterFromEntity;

		[ReadOnly]
		public BufferLookup<Employee> m_EmployeeFromEntity;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizenFromEntity;

		public NativeArray<float2> m_Result;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityHandle);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefHandle);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				float2 @float = new float2(0f, 0f);
				ServiceObjectData componentData3;
				MailAccumulationData componentData4;
				if (m_SpawnableDataFromEntity.TryGetComponent(prefabRef.m_Prefab, out var componentData))
				{
					if (m_MailAccumulationFromEntity.TryGetComponent(componentData.m_ZonePrefab, out var componentData2))
					{
						@float = componentData2.m_AccumulationRate;
					}
				}
				else if (m_ServiceObjectFromEntity.TryGetComponent(prefabRef.m_Prefab, out componentData3) && m_MailAccumulationFromEntity.TryGetComponent(componentData3.m_Service, out componentData4))
				{
					@float = componentData4.m_AccumulationRate;
				}
				int num = 0;
				if (m_RenterFromEntity.TryGetBuffer(entity, out var bufferData))
				{
					if (bufferData.Length > 0)
					{
						GetCitizenCounts(bufferData, m_HouseholdCitizenFromEntity, m_EmployeeFromEntity, out var residentCount, out var workerCount);
						num += residentCount + workerCount;
					}
				}
				else
				{
					GetCitizenCounts(entity, m_HouseholdCitizenFromEntity, m_EmployeeFromEntity, out var residentCount2, out var workerCount2);
					num += residentCount2 + workerCount2;
				}
				m_Result[0] += @float * num;
			}
		}

		private void GetCitizenCounts(DynamicBuffer<Renter> renters, BufferLookup<HouseholdCitizen> citizens, BufferLookup<Employee> employees, out int residentCount, out int workerCount)
		{
			residentCount = 0;
			workerCount = 0;
			for (int i = 0; i < renters.Length; i++)
			{
				GetCitizenCounts(renters[i].m_Renter, citizens, employees, out var residentCount2, out var workerCount2);
				residentCount += residentCount2;
				workerCount += workerCount2;
			}
		}

		private void GetCitizenCounts(Entity entity, BufferLookup<HouseholdCitizen> citizens, BufferLookup<Employee> employees, out int residentCount, out int workerCount)
		{
			residentCount = (citizens.HasBuffer(entity) ? citizens[entity].Length : 0);
			workerCount = (employees.HasBuffer(entity) ? employees[entity].Length : 0);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private const string kGroup = "postInfo";

	private const float kAccumulationFactor = 72.81778f;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private MailAccumulationSystem m_MailAccumulationSystem;

	private EntityQuery m_PostFacilityModifiedQuery;

	private EntityQuery m_MailProducerQuery;

	private EntityQuery m_MailProducerModifiedQuery;

	private ValueBinding<int> m_CollectedMail;

	private ValueBinding<int> m_DeliveredMail;

	private ValueBinding<float> m_MailProductionRate;

	private ValueBinding<IndicatorValue> m_PostServiceAvailability;

	private NativeArray<float2> m_Result;

	protected override bool Active
	{
		get
		{
			if (!base.Active && !m_CollectedMail.active && !m_DeliveredMail.active && !m_MailProductionRate.active)
			{
				return m_PostServiceAvailability.active;
			}
			return true;
		}
	}

	protected override bool Modified
	{
		get
		{
			if (m_MailProducerModifiedQuery.IsEmptyIgnoreFilter)
			{
				return !m_PostFacilityModifiedQuery.IsEmptyIgnoreFilter;
			}
			return true;
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_MailAccumulationSystem = base.World.GetOrCreateSystemManaged<MailAccumulationSystem>();
		m_PostFacilityModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Game.Buildings.PostFacility>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Created>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		m_MailProducerQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.ReadOnly<MailProducer>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>());
		m_MailProducerModifiedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<MailProducer>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Updated>(),
				ComponentType.ReadOnly<Created>()
			},
			None = new ComponentType[1] { ComponentType.ReadOnly<Temp>() }
		});
		AddBinding(m_CollectedMail = new ValueBinding<int>("postInfo", "collectedMail", 0));
		AddBinding(m_DeliveredMail = new ValueBinding<int>("postInfo", "deliveredMail", 0));
		AddBinding(m_MailProductionRate = new ValueBinding<float>("postInfo", "mailProductionRate", 0f));
		AddBinding(m_PostServiceAvailability = new ValueBinding<IndicatorValue>("postInfo", "postServiceAvailability", default(IndicatorValue), new ValueWriter<IndicatorValue>()));
		m_Result = new NativeArray<float2>(1, Allocator.Persistent);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_Result.Dispose();
		base.OnDestroy();
	}

	protected override void PerformUpdate()
	{
		UpdateMailRate();
		UpdateProcessingRate();
		UpdateAvailability();
	}

	private void ResetResults()
	{
		for (int i = 0; i < m_Result.Length; i++)
		{
			m_Result[i] = default(float2);
		}
	}

	private void UpdateMailRate()
	{
	}

	private void UpdateProcessingRate()
	{
		int lastProcessedMail = m_MailAccumulationSystem.LastProcessedMail;
		if (m_CityStatisticsSystem.GetStatisticValue(StatisticType.DeliveredMail) > 0)
		{
			m_DeliveredMail.Update(lastProcessedMail / 2);
			m_CollectedMail.Update(lastProcessedMail / 2);
		}
		else
		{
			m_DeliveredMail.Update(0);
			m_CollectedMail.Update(0);
		}
		m_MailProductionRate.Update(m_MailAccumulationSystem.LastAccumulatedMail);
	}

	private void UpdateAvailability()
	{
		m_PostServiceAvailability.Update(IndicatorValue.Calculate(m_DeliveredMail.value + m_CollectedMail.value, m_MailProductionRate.value));
	}

	[Preserve]
	public PostInfoviewUISystem()
	{
	}
}
