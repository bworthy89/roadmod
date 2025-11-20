#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using System.Threading;
using Colossal.Collections;
using Colossal.Entities;
using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Companies;
using Game.Notifications;
using Game.Objects;
using Game.Prefabs;
using Game.Serialization;
using Game.Tools;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class GarbageAccumulationSystem : GameSystemBase, IDefaultSerializable, ISerializable, IPreDeserialize
{
	[BurstCompile]
	private struct GarbageAccumulationJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> m_CurrentDistrictType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		public ComponentTypeHandle<GarbageProducer> m_GarbageProducerType;

		[ReadOnly]
		public ComponentLookup<GarbageCollectionRequest> m_GarbageCollectionRequestData;

		[ReadOnly]
		public ComponentLookup<Citizen> m_Citizens;

		[ReadOnly]
		public ComponentLookup<Quantity> m_QuantityData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_Prefabs;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> m_ConsumptionDatas;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_SpawnableDatas;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_ZoneDatas;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> m_HomelessHousehold;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> m_HouseholdCitizens;

		[ReadOnly]
		public BufferLookup<Employee> m_Employees;

		[ReadOnly]
		public BufferLookup<Renter> m_Renters;

		[ReadOnly]
		public BufferLookup<Game.Buildings.Student> m_Students;

		[ReadOnly]
		public BufferLookup<Occupant> m_Occupants;

		[ReadOnly]
		public BufferLookup<Patient> m_Patients;

		[ReadOnly]
		public BufferLookup<DistrictModifier> m_DistrictModifiers;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public Entity m_City;

		[ReadOnly]
		public int m_UpdateFrame;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_CollectionRequestArchetype;

		[ReadOnly]
		public GarbageParameterData m_GarbageParameters;

		[ReadOnly]
		public float m_GarbageEfficiencyPenalty;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public IconCommandBuffer m_IconCommandBuffer;

		public NativeArray<long> m_GarbageAccumulation;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<GarbageProducer> nativeArray2 = chunk.GetNativeArray(ref m_GarbageProducerType);
			NativeArray<CurrentDistrict> nativeArray3 = chunk.GetNativeArray(ref m_CurrentDistrictType);
			BufferAccessor<InstalledUpgrade> bufferAccessor = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<Efficiency> bufferAccessor2 = chunk.GetBufferAccessor(ref m_EfficiencyType);
			DynamicBuffer<CityModifier> cityModifiers = m_CityModifiers[m_City];
			long num = 0L;
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				ref GarbageProducer reference = ref nativeArray2.ElementAt(i);
				CurrentDistrict currentDistrict = nativeArray3[i];
				Entity prefab = m_Prefabs[entity].m_Prefab;
				m_ConsumptionDatas.TryGetComponent(prefab, out var componentData);
				if (bufferAccessor.Length != 0)
				{
					UpgradeUtils.CombineStats(ref componentData, bufferAccessor[i], ref m_Prefabs, ref m_ConsumptionDatas);
				}
				GetGarbageAccumulation(entity, prefab, ref componentData, currentDistrict, cityModifiers, m_Citizens, m_SpawnableDatas, m_ZoneDatas, m_HomelessHousehold, m_HouseholdCitizens, m_Renters, m_Employees, m_Students, m_Occupants, m_Patients, m_DistrictModifiers, ref m_GarbageParameters);
				GarbageCollectionRequestFlags garbageCollectionRequestFlags = (GarbageCollectionRequestFlags)0;
				if (m_SpawnableDatas.HasComponent(prefab))
				{
					SpawnableBuildingData spawnableBuildingData = m_SpawnableDatas[prefab];
					if (m_ZoneDatas.HasComponent(spawnableBuildingData.m_ZonePrefab) && m_ZoneDatas[spawnableBuildingData.m_ZonePrefab].m_AreaType == Game.Zones.AreaType.Industrial)
					{
						garbageCollectionRequestFlags |= GarbageCollectionRequestFlags.IndustrialWaste;
					}
				}
				int garbage = reference.m_Garbage;
				int num2 = MathUtils.RoundToIntRandom(ref random, componentData.m_GarbageAccumulation / (float)kUpdatesPerDay);
				reference.m_Garbage += num2;
				reference.m_Garbage = math.min(reference.m_Garbage, m_GarbageParameters.m_MaxGarbageAccumulation);
				num += num2;
				RequestCollectionIfNeeded(unfilteredChunkIndex, entity, ref reference, garbageCollectionRequestFlags);
				AddWarningIfNeeded(entity, ref reference, garbage);
				if (garbage >= m_GarbageParameters.m_RequestGarbageLimit != reference.m_Garbage >= m_GarbageParameters.m_RequestGarbageLimit || garbage >= m_GarbageParameters.m_WarningGarbageLimit != reference.m_Garbage >= m_GarbageParameters.m_WarningGarbageLimit)
				{
					QuantityUpdated(unfilteredChunkIndex, entity);
				}
				if (bufferAccessor2.Length != 0)
				{
					float garbageEfficiencyFactor = GetGarbageEfficiencyFactor(reference.m_Garbage, m_GarbageParameters, m_GarbageEfficiencyPenalty);
					BuildingUtils.SetEfficiencyFactor(bufferAccessor2[i], EfficiencyFactor.Garbage, garbageEfficiencyFactor);
				}
			}
			AddGarbageAccumulation(num);
		}

		private void QuantityUpdated(int jobIndex, Entity buildingEntity, bool updateAll = false)
		{
			if (!m_SubObjects.TryGetBuffer(buildingEntity, out var bufferData))
			{
				return;
			}
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity subObject = bufferData[i].m_SubObject;
				bool updateAll2 = false;
				if (updateAll || m_QuantityData.HasComponent(subObject))
				{
					m_CommandBuffer.AddComponent(jobIndex, subObject, default(BatchesUpdated));
					updateAll2 = true;
				}
				QuantityUpdated(jobIndex, subObject, updateAll2);
			}
		}

		private unsafe void AddGarbageAccumulation(long accumulation)
		{
			long* unsafePtr = (long*)m_GarbageAccumulation.GetUnsafePtr();
			unsafePtr += m_UpdateFrame;
			Interlocked.Add(ref *unsafePtr, accumulation);
		}

		private void RequestCollectionIfNeeded(int jobIndex, Entity entity, ref GarbageProducer garbage, GarbageCollectionRequestFlags flags)
		{
			if (garbage.m_Garbage > m_GarbageParameters.m_RequestGarbageLimit && (!m_GarbageCollectionRequestData.TryGetComponent(garbage.m_CollectionRequest, out var componentData) || (!(componentData.m_Target == entity) && componentData.m_DispatchIndex != garbage.m_DispatchIndex)))
			{
				garbage.m_CollectionRequest = Entity.Null;
				garbage.m_DispatchIndex = 0;
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_CollectionRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new GarbageCollectionRequest(entity, garbage.m_Garbage, flags));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
			}
		}

		private void AddWarningIfNeeded(Entity entity, ref GarbageProducer garbage, int oldGarbage)
		{
			if (garbage.m_Garbage > m_GarbageParameters.m_WarningGarbageLimit && oldGarbage <= m_GarbageParameters.m_WarningGarbageLimit)
			{
				m_IconCommandBuffer.Add(entity, m_GarbageParameters.m_GarbageNotificationPrefab, IconPriority.Problem);
				garbage.m_Flags |= GarbageProducerFlags.GarbagePilingUpWarning;
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RW_BufferTypeHandle;

		public ComponentTypeHandle<GarbageProducer> __Game_Buildings_GarbageProducer_RW_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<GarbageCollectionRequest> __Game_Simulation_GarbageCollectionRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Quantity> __Game_Objects_Quantity_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ConsumptionData> __Game_Prefabs_ConsumptionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<HouseholdCitizen> __Game_Citizens_HouseholdCitizen_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Employee> __Game_Companies_Employee_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Renter> __Game_Buildings_Renter_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Buildings.Student> __Game_Buildings_Student_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Occupant> __Game_Buildings_Occupant_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Patient> __Game_Buildings_Patient_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<DistrictModifier> __Game_Areas_DistrictModifier_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CurrentDistrict>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Buildings_Efficiency_RW_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>();
			__Game_Buildings_GarbageProducer_RW_ComponentTypeHandle = state.GetComponentTypeHandle<GarbageProducer>();
			__Game_Simulation_GarbageCollectionRequest_RO_ComponentLookup = state.GetComponentLookup<GarbageCollectionRequest>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Objects_Quantity_RO_ComponentLookup = state.GetComponentLookup<Quantity>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_ConsumptionData_RO_ComponentLookup = state.GetComponentLookup<ConsumptionData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
			__Game_Citizens_HouseholdCitizen_RO_BufferLookup = state.GetBufferLookup<HouseholdCitizen>(isReadOnly: true);
			__Game_Companies_Employee_RO_BufferLookup = state.GetBufferLookup<Employee>(isReadOnly: true);
			__Game_Buildings_Renter_RO_BufferLookup = state.GetBufferLookup<Renter>(isReadOnly: true);
			__Game_Buildings_Student_RO_BufferLookup = state.GetBufferLookup<Game.Buildings.Student>(isReadOnly: true);
			__Game_Buildings_Occupant_RO_BufferLookup = state.GetBufferLookup<Occupant>(isReadOnly: true);
			__Game_Buildings_Patient_RO_BufferLookup = state.GetBufferLookup<Patient>(isReadOnly: true);
			__Game_Areas_DistrictModifier_RO_BufferLookup = state.GetBufferLookup<DistrictModifier>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
		}
	}

	public static readonly int kUpdatesPerDay = 16;

	private SimulationSystem m_SimulationSystem;

	private IconCommandSystem m_IconCommandSystem;

	private CitySystem m_CitySystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private EntityQuery m_GarbageProducerQuery;

	private EntityArchetype m_CollectionRequestArchetype;

	private NativeArray<long> m_GarbageAccumulation;

	private JobHandle m_AccumulationDeps;

	private long m_Accumulation;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_2138252455_0;

	private EntityQuery __query_2138252455_1;

	public long garbageAccumulation => m_Accumulation * kUpdatesPerDay;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 262144 / (kUpdatesPerDay * 16);
	}

	public static void GetGarbage(ref ConsumptionData consumption, Entity building, Entity prefab, BufferLookup<Renter> renters, BufferLookup<Game.Buildings.Student> students, BufferLookup<Occupant> occupants, ComponentLookup<HomelessHousehold> homelessHouseholds, BufferLookup<HouseholdCitizen> householdCitizens, ComponentLookup<Citizen> citizens, BufferLookup<Employee> employees, BufferLookup<Patient> patients, ComponentLookup<SpawnableBuildingData> spawnableDatas, ComponentLookup<CurrentDistrict> currentDistricts, BufferLookup<DistrictModifier> districtModifiers, ComponentLookup<ZoneData> zoneDatas, DynamicBuffer<CityModifier> cityModifiers, ref GarbageParameterData garbageParameter)
	{
		CurrentDistrict currentDistrict = currentDistricts[building];
		GetGarbageAccumulation(building, prefab, ref consumption, currentDistrict, cityModifiers, citizens, spawnableDatas, zoneDatas, homelessHouseholds, householdCitizens, renters, employees, students, occupants, patients, districtModifiers, ref garbageParameter);
	}

	public static void GetGarbageAccumulation(Entity building, Entity prefab, ref ConsumptionData consumption, CurrentDistrict currentDistrict, DynamicBuffer<CityModifier> cityModifiers, ComponentLookup<Citizen> citizens, ComponentLookup<SpawnableBuildingData> spawnableDatas, ComponentLookup<ZoneData> zoneDatas, ComponentLookup<HomelessHousehold> homelessHousehold, BufferLookup<HouseholdCitizen> householdCitizens, BufferLookup<Renter> renters, BufferLookup<Employee> employees, BufferLookup<Game.Buildings.Student> students, BufferLookup<Occupant> occupants, BufferLookup<Patient> patients, BufferLookup<DistrictModifier> districtModifiers, ref GarbageParameterData garbageParameter)
	{
		float num = 0f;
		int num2 = 0;
		float num3 = 0f;
		if (renters.HasBuffer(building))
		{
			DynamicBuffer<Renter> dynamicBuffer = renters[building];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity renter = dynamicBuffer[i].m_Renter;
				if (householdCitizens.HasBuffer(renter))
				{
					DynamicBuffer<HouseholdCitizen> dynamicBuffer2 = householdCitizens[renter];
					if (homelessHousehold.HasComponent(renter))
					{
						num2 += dynamicBuffer2.Length;
						continue;
					}
					for (int j = 0; j < dynamicBuffer2.Length; j++)
					{
						Entity citizen = dynamicBuffer2[j].m_Citizen;
						if (citizens.HasComponent(citizen))
						{
							num3 += (float)citizens[citizen].GetEducationLevel();
							num += 1f;
						}
					}
				}
				else
				{
					if (!employees.HasBuffer(renter))
					{
						continue;
					}
					DynamicBuffer<Employee> dynamicBuffer3 = employees[renter];
					for (int k = 0; k < dynamicBuffer3.Length; k++)
					{
						Entity worker = dynamicBuffer3[k].m_Worker;
						if (citizens.HasComponent(worker))
						{
							num3 += (float)citizens[worker].GetEducationLevel();
							num += 1f;
						}
					}
				}
			}
			if (employees.HasBuffer(building))
			{
				DynamicBuffer<Employee> dynamicBuffer4 = employees[building];
				for (int l = 0; l < dynamicBuffer4.Length; l++)
				{
					Entity worker2 = dynamicBuffer4[l].m_Worker;
					if (citizens.HasComponent(worker2))
					{
						num3 += (float)citizens[worker2].GetEducationLevel();
						num += 1f;
					}
				}
			}
		}
		else
		{
			if (employees.HasBuffer(building))
			{
				DynamicBuffer<Employee> dynamicBuffer5 = employees[building];
				for (int m = 0; m < dynamicBuffer5.Length; m++)
				{
					Entity worker3 = dynamicBuffer5[m].m_Worker;
					if (citizens.HasComponent(worker3))
					{
						num3 += (float)citizens[worker3].GetEducationLevel();
						num += 1f;
					}
				}
			}
			if (students.HasBuffer(building))
			{
				DynamicBuffer<Game.Buildings.Student> dynamicBuffer6 = students[building];
				for (int n = 0; n < dynamicBuffer6.Length; n++)
				{
					Entity entity = dynamicBuffer6[n];
					if (citizens.HasComponent(entity))
					{
						num3 += (float)citizens[entity].GetEducationLevel();
						num += 1f;
					}
				}
			}
			if (occupants.HasBuffer(building))
			{
				DynamicBuffer<Occupant> dynamicBuffer7 = occupants[building];
				for (int num4 = 0; num4 < dynamicBuffer7.Length; num4++)
				{
					Entity entity2 = dynamicBuffer7[num4];
					if (citizens.HasComponent(entity2))
					{
						num3 += (float)citizens[entity2].GetEducationLevel();
						num += 1f;
					}
				}
			}
			if (patients.HasBuffer(building))
			{
				DynamicBuffer<Patient> dynamicBuffer8 = patients[building];
				for (int num5 = 0; num5 < dynamicBuffer8.Length; num5++)
				{
					Entity patient = dynamicBuffer8[num5].m_Patient;
					if (citizens.HasComponent(patient))
					{
						num3 += (float)citizens[patient].GetEducationLevel();
						num += 1f;
					}
				}
			}
		}
		float num6 = 0f;
		if (spawnableDatas.HasComponent(prefab))
		{
			num6 = (int)spawnableDatas[prefab].m_Level;
		}
		float num7 = 0f;
		num7 = ((!(num > 0f)) ? (consumption.m_GarbageAccumulation - num6 * garbageParameter.m_BuildingLevelBalance) : (math.max(0f, consumption.m_GarbageAccumulation - (num6 * garbageParameter.m_BuildingLevelBalance + num3 / num * garbageParameter.m_EducationBalance)) * num));
		if (num2 > 0)
		{
			num7 += (float)(garbageParameter.m_HomelessGarbageProduce * num2);
		}
		if (districtModifiers.HasBuffer(currentDistrict.m_District))
		{
			DynamicBuffer<DistrictModifier> modifiers = districtModifiers[currentDistrict.m_District];
			AreaUtils.ApplyModifier(ref num7, modifiers, DistrictModifierType.GarbageProduction);
		}
		if (spawnableDatas.HasComponent(prefab))
		{
			SpawnableBuildingData spawnableBuildingData = spawnableDatas[prefab];
			if (zoneDatas.HasComponent(spawnableBuildingData.m_ZonePrefab) && zoneDatas[spawnableBuildingData.m_ZonePrefab].m_AreaType == Game.Zones.AreaType.Industrial && (zoneDatas[spawnableBuildingData.m_ZonePrefab].m_ZoneFlags & ZoneFlags.Office) == 0)
			{
				CityUtils.ApplyModifier(ref num7, cityModifiers, CityModifierType.IndustrialGarbage);
			}
		}
		consumption.m_GarbageAccumulation = num7;
	}

	public static float GetGarbageEfficiencyFactor(int garbage, GarbageParameterData garbageParameters, float maxPenalty)
	{
		float num = math.saturate((float)(garbage - garbageParameters.m_WarningGarbageLimit) / (float)(garbageParameters.m_MaxGarbageAccumulation - garbageParameters.m_WarningGarbageLimit));
		return 1f - maxPenalty * num;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_GarbageAccumulation = new NativeArray<long>(16, Allocator.Persistent);
		m_GarbageProducerQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<GarbageProducer>(),
				ComponentType.ReadOnly<PrefabRef>(),
				ComponentType.ReadOnly<UpdateFrame>()
			},
			None = new ComponentType[3]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_CollectionRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<GarbageCollectionRequest>(), ComponentType.ReadWrite<RequestGroup>());
		RequireForUpdate(m_GarbageProducerQuery);
		RequireForUpdate<GarbageParameterData>();
		RequireForUpdate<BuildingEfficiencyParameterData>();
		Assert.IsTrue((long)(262144 / kUpdatesPerDay) >= 512L);
	}

	[Preserve]
	protected override void OnDestroy()
	{
		base.OnDestroy();
		m_GarbageAccumulation.Dispose();
	}

	public void PreDeserialize(Context context)
	{
		m_Accumulation = 0L;
		for (int i = 0; i < m_GarbageAccumulation.Length; i++)
		{
			m_GarbageAccumulation[i] = 0L;
		}
	}

	[Preserve]
	protected override void OnUpdate()
	{
		uint updateFrame = SimulationUtils.GetUpdateFrame(m_SimulationSystem.frameIndex, kUpdatesPerDay, 16);
		m_AccumulationDeps.Complete();
		long num = 0L;
		for (int i = 0; i < 16; i++)
		{
			num += m_GarbageAccumulation[i];
		}
		m_Accumulation = num;
		m_GarbageAccumulation[(int)updateFrame] = 0L;
		GarbageParameterData singleton = __query_2138252455_0.GetSingleton<GarbageParameterData>();
		if (!base.EntityManager.HasEnabledComponent<Locked>(singleton.m_GarbageServicePrefab))
		{
			m_GarbageProducerQuery.ResetFilter();
			m_GarbageProducerQuery.SetSharedComponentFilter(new UpdateFrame(updateFrame));
			JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new GarbageAccumulationJob
			{
				m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
				m_CurrentDistrictType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentTypeHandle, ref base.CheckedStateRef),
				m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
				m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RW_BufferTypeHandle, ref base.CheckedStateRef),
				m_GarbageProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_GarbageProducer_RW_ComponentTypeHandle, ref base.CheckedStateRef),
				m_GarbageCollectionRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_GarbageCollectionRequest_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Citizens = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
				m_QuantityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Quantity_RO_ComponentLookup, ref base.CheckedStateRef),
				m_Prefabs = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ConsumptionDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ConsumptionData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_SpawnableDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_ZoneDatas = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HomelessHousehold = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
				m_HouseholdCitizens = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Citizens_HouseholdCitizen_RO_BufferLookup, ref base.CheckedStateRef),
				m_Employees = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Companies_Employee_RO_BufferLookup, ref base.CheckedStateRef),
				m_Renters = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Renter_RO_BufferLookup, ref base.CheckedStateRef),
				m_Students = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Student_RO_BufferLookup, ref base.CheckedStateRef),
				m_Occupants = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Occupant_RO_BufferLookup, ref base.CheckedStateRef),
				m_Patients = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Patient_RO_BufferLookup, ref base.CheckedStateRef),
				m_DistrictModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_DistrictModifier_RO_BufferLookup, ref base.CheckedStateRef),
				m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
				m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
				m_City = m_CitySystem.City,
				m_UpdateFrame = (int)updateFrame,
				m_RandomSeed = RandomSeed.Next(),
				m_CollectionRequestArchetype = m_CollectionRequestArchetype,
				m_GarbageParameters = singleton,
				m_GarbageEfficiencyPenalty = __query_2138252455_1.GetSingleton<BuildingEfficiencyParameterData>().m_GarbagePenalty,
				m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
				m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
				m_GarbageAccumulation = m_GarbageAccumulation
			}, m_GarbageProducerQuery, base.Dependency);
			m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
			m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
			base.Dependency = jobHandle;
			m_AccumulationDeps = jobHandle;
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		byte value = (byte)m_GarbageAccumulation.Length;
		writer.Write(value);
		for (int i = 0; i < m_GarbageAccumulation.Length; i++)
		{
			long value2 = m_GarbageAccumulation[i];
			writer.Write(value2);
		}
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		m_Accumulation = 0L;
		reader.Read(out byte value);
		for (int i = 0; i < value; i++)
		{
			reader.Read(out long value2);
			if (i < m_GarbageAccumulation.Length)
			{
				m_GarbageAccumulation[i] = value2;
				m_Accumulation += value2;
			}
		}
		for (int j = value; j < m_GarbageAccumulation.Length; j++)
		{
			m_GarbageAccumulation[j] = 0L;
		}
	}

	public void SetDefaults(Context context)
	{
		m_Accumulation = 0L;
		for (int i = 0; i < m_GarbageAccumulation.Length; i++)
		{
			m_GarbageAccumulation[i] = 0L;
		}
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<GarbageParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_2138252455_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<BuildingEfficiencyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_2138252455_1 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder.Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public GarbageAccumulationSystem()
	{
	}
}
