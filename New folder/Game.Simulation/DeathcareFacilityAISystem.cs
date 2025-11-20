#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.Vehicles;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class DeathcareFacilityAISystem : GameSystemBase
{
	private struct DeathcareFacilityAction
	{
		public Entity m_Entity;

		public bool m_Disabled;

		public static DeathcareFacilityAction SetDisabled(Entity vehicle, bool disabled)
		{
			return new DeathcareFacilityAction
			{
				m_Entity = vehicle,
				m_Disabled = disabled
			};
		}
	}

	[BurstCompile]
	private struct DeathcareFacilityTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicleType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		public ComponentTypeHandle<Game.Buildings.DeathcareFacility> m_DeathcareFacilityType;

		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		public BufferTypeHandle<Patient> m_PatientType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public ComponentLookup<DeathcareFacilityData> m_PrefabDeathcareFacilityData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefDataFromEntity;

		[ReadOnly]
		public ComponentLookup<HealthcareRequest> m_HealthcareRequestData;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> m_ServiceRequestData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Hearse> m_HearseData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> m_CurrentBuildingData;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> m_CurrentTransportData;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public HealthcareParameterData m_HealthcareParameters;

		[ReadOnly]
		public EntityArchetype m_HealthcareRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingCarAddTypes;

		[ReadOnly]
		public HealthcareVehicleSelectData m_HealthcareVehicleSelectData;

		public IconCommandBuffer m_IconCommandBuffer;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<DeathcareFacilityAction>.ParallelWriter m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<Game.Buildings.DeathcareFacility> nativeArray3 = chunk.GetNativeArray(ref m_DeathcareFacilityType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			BufferAccessor<OwnedVehicle> bufferAccessor2 = chunk.GetBufferAccessor(ref m_OwnedVehicleType);
			BufferAccessor<Patient> bufferAccessor3 = chunk.GetBufferAccessor(ref m_PatientType);
			BufferAccessor<ServiceDispatch> bufferAccessor4 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			BufferAccessor<InstalledUpgrade> bufferAccessor5 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				Game.Buildings.DeathcareFacility facility = nativeArray3[i];
				DynamicBuffer<OwnedVehicle> vehicles = bufferAccessor2[i];
				DynamicBuffer<ServiceDispatch> dispatches = bufferAccessor4[i];
				DynamicBuffer<Patient> patients = default(DynamicBuffer<Patient>);
				if (bufferAccessor3.Length != 0)
				{
					patients = bufferAccessor3[i];
				}
				DeathcareFacilityData data = default(DeathcareFacilityData);
				if (m_PrefabDeathcareFacilityData.HasComponent(prefabRef.m_Prefab))
				{
					data = m_PrefabDeathcareFacilityData[prefabRef.m_Prefab];
				}
				if (bufferAccessor5.Length != 0)
				{
					UpgradeUtils.CombineStats(ref data, bufferAccessor5[i], ref m_PrefabRefDataFromEntity, ref m_PrefabDeathcareFacilityData);
				}
				float efficiency = BuildingUtils.GetEfficiency(bufferAccessor, i);
				float immediateEfficiency = BuildingUtils.GetImmediateEfficiency(bufferAccessor, i);
				Tick(unfilteredChunkIndex, entity, ref facility, data, vehicles, patients, dispatches, efficiency, immediateEfficiency);
				nativeArray3[i] = facility;
			}
		}

		private void Tick(int jobIndex, Entity entity, ref Game.Buildings.DeathcareFacility facility, DeathcareFacilityData prefabDeathcareFacilityData, DynamicBuffer<OwnedVehicle> vehicles, DynamicBuffer<Patient> patients, DynamicBuffer<ServiceDispatch> dispatches, float efficiency, float immediateEfficiency)
		{
			Random random = m_RandomSeed.GetRandom(entity.Index);
			int vehicleCapacity = BuildingUtils.GetVehicleCapacity(math.min(efficiency, immediateEfficiency), prefabDeathcareFacilityData.m_HearseCapacity);
			int num = BuildingUtils.GetVehicleCapacity(immediateEfficiency, prefabDeathcareFacilityData.m_HearseCapacity);
			int availableVehicles = vehicleCapacity;
			facility.m_ProcessingState += efficiency * prefabDeathcareFacilityData.m_ProcessingRate * 0.0009765625f;
			StackList<Entity> parkedVehicles = stackalloc Entity[vehicles.Length];
			for (int i = 0; i < vehicles.Length; i++)
			{
				Entity vehicle = vehicles[i].m_Vehicle;
				if (!m_HearseData.TryGetComponent(vehicle, out var componentData))
				{
					continue;
				}
				if (m_ParkedCarData.TryGetComponent(vehicle, out var componentData2))
				{
					if (!m_EntityLookup.Exists(componentData2.m_Lane))
					{
						m_CommandBuffer.AddComponent<Deleted>(jobIndex, vehicle);
					}
					else
					{
						parkedVehicles.AddNoResize(vehicle);
					}
					continue;
				}
				availableVehicles--;
				bool flag = --num < 0;
				if ((componentData.m_State & HearseFlags.Disabled) != 0 != flag)
				{
					m_ActionQueue.Enqueue(DeathcareFacilityAction.SetDisabled(vehicle, flag));
				}
			}
			int num2 = 0;
			while (num2 < dispatches.Length)
			{
				Entity request = dispatches[num2].m_Request;
				if (m_HealthcareRequestData.TryGetComponent(request, out var componentData3))
				{
					if (componentData3.m_Type == HealthcareRequestType.Hearse)
					{
						SpawnVehicle(jobIndex, ref random, entity, request, ref facility, ref availableVehicles, ref parkedVehicles);
						dispatches.RemoveAt(num2);
					}
					else
					{
						num2++;
					}
				}
				else if (!m_ServiceRequestData.HasComponent(request))
				{
					dispatches.RemoveAt(num2);
				}
				else
				{
					num2++;
				}
			}
			while (parkedVehicles.Length > math.max(0, prefabDeathcareFacilityData.m_HearseCapacity + availableVehicles - vehicleCapacity))
			{
				int index = random.NextInt(parkedVehicles.Length);
				m_CommandBuffer.AddComponent<Deleted>(jobIndex, parkedVehicles[index]);
				parkedVehicles.RemoveAtSwapBack(index);
			}
			for (int j = 0; j < parkedVehicles.Length; j++)
			{
				Entity entity2 = parkedVehicles[j];
				Game.Vehicles.Hearse hearse = m_HearseData[entity2];
				bool flag2 = availableVehicles <= 0;
				if ((hearse.m_State & HearseFlags.Disabled) != 0 != flag2)
				{
					m_ActionQueue.Enqueue(DeathcareFacilityAction.SetDisabled(entity2, flag2));
				}
			}
			facility.m_Flags &= ~(DeathcareFacilityFlags.HasAvailableHearses | DeathcareFacilityFlags.HasRoomForBodies | DeathcareFacilityFlags.CanProcessCorpses | DeathcareFacilityFlags.CanStoreCorpses);
			if (availableVehicles != 0)
			{
				facility.m_Flags |= DeathcareFacilityFlags.HasAvailableHearses;
			}
			if (prefabDeathcareFacilityData.m_ProcessingRate > 0f)
			{
				facility.m_Flags |= DeathcareFacilityFlags.CanProcessCorpses;
			}
			if (prefabDeathcareFacilityData.m_StorageCapacity > 0)
			{
				facility.m_Flags |= DeathcareFacilityFlags.CanStoreCorpses;
			}
			while (facility.m_LongTermStoredCount > 0 && facility.m_ProcessingState >= 1f)
			{
				facility.m_ProcessingState -= 1f;
				facility.m_LongTermStoredCount--;
			}
			if (patients.IsCreated)
			{
				int num3 = 0;
				while (num3 < patients.Length)
				{
					Entity patient = patients[num3].m_Patient;
					if (!m_PrefabRefDataFromEntity.HasComponent(patient))
					{
						patients.RemoveAt(num3);
					}
					else if (facility.m_ProcessingState >= 1f)
					{
						facility.m_ProcessingState -= 1f;
						m_CommandBuffer.AddComponent(jobIndex, patient, default(Deleted));
						patients.RemoveAt(num3);
					}
					else if (prefabDeathcareFacilityData.m_LongTermStorage)
					{
						facility.m_LongTermStoredCount++;
						m_CommandBuffer.AddComponent(jobIndex, patient, default(Deleted));
						patients.RemoveAt(num3);
					}
					else
					{
						num3++;
					}
				}
				int num4 = facility.m_LongTermStoredCount + patients.Length;
				if (num4 < prefabDeathcareFacilityData.m_StorageCapacity)
				{
					facility.m_Flags |= DeathcareFacilityFlags.HasRoomForBodies;
				}
				if (num4 == 0)
				{
					facility.m_ProcessingState = 0f;
				}
				if (prefabDeathcareFacilityData.m_LongTermStorage)
				{
					if (num4 >= prefabDeathcareFacilityData.m_StorageCapacity)
					{
						if ((facility.m_Flags & DeathcareFacilityFlags.IsFull) == 0)
						{
							m_IconCommandBuffer.Add(entity, m_HealthcareParameters.m_FacilityFullNotificationPrefab);
							facility.m_Flags |= DeathcareFacilityFlags.IsFull;
						}
					}
					else if ((facility.m_Flags & DeathcareFacilityFlags.IsFull) != 0)
					{
						m_IconCommandBuffer.Remove(entity, m_HealthcareParameters.m_FacilityFullNotificationPrefab);
						facility.m_Flags &= ~DeathcareFacilityFlags.IsFull;
					}
				}
			}
			else
			{
				facility.m_ProcessingState = 0f;
			}
			if ((facility.m_Flags & (DeathcareFacilityFlags.HasAvailableHearses | DeathcareFacilityFlags.HasRoomForBodies)) == (DeathcareFacilityFlags.HasAvailableHearses | DeathcareFacilityFlags.HasRoomForBodies))
			{
				RequestTargetIfNeeded(jobIndex, entity, ref facility);
			}
		}

		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Buildings.DeathcareFacility deathcareFacility)
		{
			if (!m_ServiceRequestData.HasComponent(deathcareFacility.m_TargetRequest))
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HealthcareRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
				m_CommandBuffer.SetComponent(jobIndex, e, new HealthcareRequest(entity, HealthcareRequestType.Hearse));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(16u));
			}
		}

		private void SpawnVehicle(int jobIndex, ref Random random, Entity entity, Entity request, ref Game.Buildings.DeathcareFacility deathcareFacility, ref int availableVehicles, ref StackList<Entity> parkedVehicles)
		{
			if (availableVehicles <= 0 || !m_HealthcareRequestData.TryGetComponent(request, out var componentData))
			{
				return;
			}
			Entity citizen = componentData.m_Citizen;
			Entity entity2 = Entity.Null;
			CurrentBuilding componentData3;
			if (m_CurrentTransportData.TryGetComponent(citizen, out var componentData2))
			{
				entity2 = componentData2.m_CurrentTransport;
			}
			else if (m_CurrentBuildingData.TryGetComponent(citizen, out componentData3))
			{
				entity2 = componentData3.m_CurrentBuilding;
			}
			if (!m_EntityLookup.Exists(entity2))
			{
				return;
			}
			Entity entity3 = Entity.Null;
			if (m_PathInformationData.TryGetComponent(request, out var componentData4) && componentData4.m_Origin != entity)
			{
				if (!CollectionUtils.RemoveValueSwapBack(ref parkedVehicles, componentData4.m_Origin))
				{
					return;
				}
				ParkedCar parkedCar = m_ParkedCarData[componentData4.m_Origin];
				entity3 = componentData4.m_Origin;
				m_CommandBuffer.RemoveComponent(jobIndex, entity3, in m_ParkedToMovingRemoveTypes);
				Game.Vehicles.CarLaneFlags flags = Game.Vehicles.CarLaneFlags.EndReached | Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.FixedLane;
				m_CommandBuffer.AddComponent(jobIndex, entity3, in m_ParkedToMovingCarAddTypes);
				m_CommandBuffer.SetComponent(jobIndex, entity3, new CarCurrentLane(parkedCar, flags));
				if (m_ParkingLaneData.HasComponent(parkedCar.m_Lane) || m_SpawnLocationData.HasComponent(parkedCar.m_Lane))
				{
					m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, parkedCar.m_Lane);
				}
			}
			if (entity3 == Entity.Null)
			{
				entity3 = m_HealthcareVehicleSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, m_TransformData[entity], entity, Entity.Null, componentData.m_Type, RoadTypes.Car, parked: false);
				if (entity3 == Entity.Null)
				{
					return;
				}
				m_CommandBuffer.AddComponent(jobIndex, entity3, new Owner(entity));
			}
			availableVehicles--;
			m_CommandBuffer.SetComponent(jobIndex, entity3, new Game.Vehicles.Hearse(citizen, HearseFlags.Dispatched));
			m_CommandBuffer.SetComponent(jobIndex, entity3, new Target(entity2));
			m_CommandBuffer.SetBuffer<ServiceDispatch>(jobIndex, entity3).Add(new ServiceDispatch(request));
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, entity3, completed: false));
			if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
			{
				DynamicBuffer<PathElement> targetElements = m_CommandBuffer.SetBuffer<PathElement>(jobIndex, entity3);
				PathUtils.CopyPath(bufferData, default(PathOwner), 0, targetElements);
				m_CommandBuffer.SetComponent(jobIndex, entity3, new PathOwner(PathFlags.Updated));
				m_CommandBuffer.SetComponent(jobIndex, entity3, componentData4);
			}
			if (m_ServiceRequestData.HasComponent(deathcareFacility.m_TargetRequest))
			{
				e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(deathcareFacility.m_TargetRequest, Entity.Null, completed: true));
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct DeathcareFacilityActionJob : IJob
	{
		public ComponentLookup<Game.Vehicles.Hearse> m_HearseData;

		public NativeQueue<DeathcareFacilityAction> m_ActionQueue;

		public void Execute()
		{
			DeathcareFacilityAction item;
			while (m_ActionQueue.TryDequeue(out item))
			{
				if (m_HearseData.TryGetComponent(item.m_Entity, out var componentData))
				{
					if (item.m_Disabled)
					{
						componentData.m_State |= HearseFlags.Disabled;
					}
					else
					{
						componentData.m_State &= ~HearseFlags.Disabled;
					}
					m_HearseData[item.m_Entity] = componentData;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle;

		public BufferTypeHandle<Patient> __Game_Buildings_Patient_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Buildings.DeathcareFacility> __Game_Buildings_DeathcareFacility_RW_ComponentTypeHandle;

		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<DeathcareFacilityData> __Game_Prefabs_DeathcareFacilityData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthcareRequest> __Game_Simulation_HealthcareRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Hearse> __Game_Vehicles_Hearse_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentLookup;

		public ComponentLookup<Game.Vehicles.Hearse> __Game_Vehicles_Hearse_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>(isReadOnly: true);
			__Game_Buildings_Patient_RW_BufferTypeHandle = state.GetBufferTypeHandle<Patient>();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_DeathcareFacility_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.DeathcareFacility>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Prefabs_DeathcareFacilityData_RO_ComponentLookup = state.GetComponentLookup<DeathcareFacilityData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Simulation_HealthcareRequest_RO_ComponentLookup = state.GetComponentLookup<HealthcareRequest>(isReadOnly: true);
			__Game_Simulation_ServiceRequest_RO_ComponentLookup = state.GetComponentLookup<ServiceRequest>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Vehicles_Hearse_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.Hearse>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_CurrentTransport_RO_ComponentLookup = state.GetComponentLookup<CurrentTransport>(isReadOnly: true);
			__Game_Vehicles_Hearse_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.Hearse>();
		}
	}

	private EntityQuery m_FacilityQuery;

	private EntityQuery m_HealthcareVehiclePrefabQuery;

	private EntityQuery m_HealthcareSettingsQuery;

	private EntityArchetype m_HealthcareRequestArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private ComponentTypeSet m_ParkedToMovingRemoveTypes;

	private ComponentTypeSet m_ParkedToMovingCarAddTypes;

	private EndFrameBarrier m_EndFrameBarrier;

	private BudgetSystem m_BudgetSystem;

	private CitySystem m_CitySystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private IconCommandSystem m_IconCommandSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private HealthcareVehicleSelectData m_HealthcareVehicleSelectData;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 32;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_BudgetSystem = base.World.GetOrCreateSystemManaged<BudgetSystem>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_HealthcareVehicleSelectData = new HealthcareVehicleSelectData(this);
		m_FacilityQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.DeathcareFacility>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_HealthcareVehiclePrefabQuery = GetEntityQuery(HealthcareVehicleSelectData.GetEntityQueryDesc());
		m_HealthcareSettingsQuery = GetEntityQuery(ComponentType.ReadOnly<HealthcareParameterData>());
		m_HealthcareRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<HealthcareRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_HandleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(), ComponentType.ReadWrite<Event>());
		m_ParkedToMovingRemoveTypes = new ComponentTypeSet(ComponentType.ReadWrite<ParkedCar>(), ComponentType.ReadWrite<Stopped>());
		m_ParkedToMovingCarAddTypes = new ComponentTypeSet(new ComponentType[14]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<CarNavigation>(),
			ComponentType.ReadWrite<CarNavigationLane>(),
			ComponentType.ReadWrite<CarCurrentLane>(),
			ComponentType.ReadWrite<PathOwner>(),
			ComponentType.ReadWrite<Target>(),
			ComponentType.ReadWrite<Blocker>(),
			ComponentType.ReadWrite<PathElement>(),
			ComponentType.ReadWrite<PathInformation>(),
			ComponentType.ReadWrite<ServiceDispatch>(),
			ComponentType.ReadWrite<Swaying>(),
			ComponentType.ReadWrite<Updated>()
		});
		RequireForUpdate(m_FacilityQuery);
		RequireForUpdate(m_HealthcareSettingsQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_HealthcareVehicleSelectData.PreUpdate(this, m_CityConfigurationSystem, m_HealthcareVehiclePrefabQuery, Allocator.TempJob, out var jobHandle);
		NativeQueue<DeathcareFacilityAction> actionQueue = new NativeQueue<DeathcareFacilityAction>(Allocator.TempJob);
		DeathcareFacilityTickJob jobData = new DeathcareFacilityTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnedVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PatientType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Patient_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeathcareFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_DeathcareFacility_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabDeathcareFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_DeathcareFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefDataFromEntity = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthcareRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_HealthcareRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HearseData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Hearse_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_HealthcareParameters = m_HealthcareSettingsQuery.GetSingleton<HealthcareParameterData>(),
			m_HealthcareRequestArchetype = m_HealthcareRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_ParkedToMovingRemoveTypes = m_ParkedToMovingRemoveTypes,
			m_ParkedToMovingCarAddTypes = m_ParkedToMovingCarAddTypes,
			m_HealthcareVehicleSelectData = m_HealthcareVehicleSelectData,
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_ActionQueue = actionQueue.AsParallelWriter()
		};
		DeathcareFacilityActionJob jobData2 = new DeathcareFacilityActionJob
		{
			m_HearseData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Hearse_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ActionQueue = actionQueue
		};
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData, m_FacilityQuery, JobHandle.CombineDependencies(base.Dependency, jobHandle));
		JobHandle jobHandle3 = IJobExtensions.Schedule(jobData2, jobHandle2);
		actionQueue.Dispose(jobHandle3);
		m_HealthcareVehicleSelectData.PostUpdate(jobHandle2);
		m_IconCommandSystem.AddCommandBufferWriter(jobHandle2);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		base.Dependency = jobHandle3;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		new EntityQueryBuilder(Allocator.Temp).Dispose();
	}

	protected override void OnCreateForCompiler()
	{
		base.OnCreateForCompiler();
		__AssignQueries(ref base.CheckedStateRef);
		__TypeHandle.__AssignHandles(ref base.CheckedStateRef);
	}

	[Preserve]
	public DeathcareFacilityAISystem()
	{
	}
}
