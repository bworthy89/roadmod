#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Routes;
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
public class PrisonAISystem : GameSystemBase
{
	private struct PrisonAction
	{
		public Entity m_Entity;

		public bool m_Disabled;

		public static PrisonAction SetDisabled(Entity vehicle, bool disabled)
		{
			return new PrisonAction
			{
				m_Entity = vehicle,
				m_Disabled = disabled
			};
		}
	}

	[BurstCompile]
	private struct PrisonTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicleType;

		public ComponentTypeHandle<Game.Buildings.Prison> m_PrisonType;

		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		public BufferTypeHandle<Occupant> m_OccupantType;

		public BufferTypeHandle<Resources> m_ResourcesType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<PrisonerTransportRequest> m_PrisonerTransportRequestData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> m_ServiceRequestData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<PrisonData> m_PrisonData;

		[ReadOnly]
		public ComponentLookup<PublicTransportVehicleData> m_PrefabPublicTransportVehicleData;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public BufferLookup<ResourceProductionData> m_ResourceProductionDatas;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_PrisonerTransportRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingCarAddTypes;

		[ReadOnly]
		public TransportVehicleSelectData m_TransportVehicleSelectData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<PrisonAction>.ParallelWriter m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Game.Buildings.Prison> nativeArray4 = chunk.GetNativeArray(ref m_PrisonType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<OwnedVehicle> bufferAccessor3 = chunk.GetBufferAccessor(ref m_OwnedVehicleType);
			BufferAccessor<ServiceDispatch> bufferAccessor4 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			BufferAccessor<Occupant> bufferAccessor5 = chunk.GetBufferAccessor(ref m_OccupantType);
			BufferAccessor<Resources> bufferAccessor6 = chunk.GetBufferAccessor(ref m_ResourcesType);
			NativeList<ResourceProductionData> resourceProductionBuffer = default(NativeList<ResourceProductionData>);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Transform transform = nativeArray2[i];
				PrefabRef prefabRef = nativeArray3[i];
				Game.Buildings.Prison prison = nativeArray4[i];
				DynamicBuffer<OwnedVehicle> vehicles = bufferAccessor3[i];
				DynamicBuffer<ServiceDispatch> dispatches = bufferAccessor4[i];
				DynamicBuffer<Occupant> occupants = bufferAccessor5[i];
				PrisonData data = default(PrisonData);
				if (m_PrisonData.HasComponent(prefabRef.m_Prefab))
				{
					data = m_PrisonData[prefabRef.m_Prefab];
				}
				if (m_ResourceProductionDatas.HasBuffer(prefabRef.m_Prefab))
				{
					AddResourceProductionData(m_ResourceProductionDatas[prefabRef.m_Prefab], ref resourceProductionBuffer);
				}
				if (bufferAccessor2.Length != 0)
				{
					DynamicBuffer<InstalledUpgrade> upgrades = bufferAccessor2[i];
					UpgradeUtils.CombineStats(ref data, upgrades, ref m_PrefabRefData, ref m_PrisonData);
					CombineResourceProductionData(upgrades, ref resourceProductionBuffer);
				}
				float efficiency = BuildingUtils.GetEfficiency(bufferAccessor, i);
				float immediateEfficiency = BuildingUtils.GetImmediateEfficiency(bufferAccessor, i);
				DynamicBuffer<Resources> resources = default(DynamicBuffer<Resources>);
				if (bufferAccessor6.Length != 0)
				{
					resources = bufferAccessor6[i];
				}
				Tick(unfilteredChunkIndex, entity, transform, ref random, ref prison, data, vehicles, dispatches, occupants, resources, resourceProductionBuffer, efficiency, immediateEfficiency);
				nativeArray4[i] = prison;
				if (resourceProductionBuffer.IsCreated)
				{
					resourceProductionBuffer.Clear();
				}
			}
			if (resourceProductionBuffer.IsCreated)
			{
				resourceProductionBuffer.Dispose();
			}
		}

		private void AddResourceProductionData(DynamicBuffer<ResourceProductionData> resourceProductionDatas, ref NativeList<ResourceProductionData> resourceProductionBuffer)
		{
			if (!resourceProductionBuffer.IsCreated)
			{
				resourceProductionBuffer = new NativeList<ResourceProductionData>(resourceProductionDatas.Length, Allocator.Temp);
			}
			ResourceProductionData.Combine(resourceProductionBuffer, resourceProductionDatas);
		}

		private void CombineResourceProductionData(DynamicBuffer<InstalledUpgrade> upgrades, ref NativeList<ResourceProductionData> resourceProductionBuffer)
		{
			for (int i = 0; i < upgrades.Length; i++)
			{
				InstalledUpgrade installedUpgrade = upgrades[i];
				if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive))
				{
					PrefabRef prefabRef = m_PrefabRefData[installedUpgrade.m_Upgrade];
					if (m_ResourceProductionDatas.TryGetBuffer(prefabRef.m_Prefab, out var bufferData))
					{
						AddResourceProductionData(bufferData, ref resourceProductionBuffer);
					}
				}
			}
		}

		private void Tick(int jobIndex, Entity entity, Transform transform, ref Random random, ref Game.Buildings.Prison prison, PrisonData prefabPrisonData, DynamicBuffer<OwnedVehicle> vehicles, DynamicBuffer<ServiceDispatch> dispatches, DynamicBuffer<Occupant> occupants, DynamicBuffer<Resources> resources, NativeList<ResourceProductionData> resourceProductionBuffer, float efficiency, float immediateEfficiency)
		{
			int num = 0;
			while (num < occupants.Length)
			{
				if (m_EntityLookup.Exists(occupants[num].m_Occupant))
				{
					num++;
				}
				else
				{
					occupants.RemoveAt(num);
				}
			}
			int vehicleCapacity = BuildingUtils.GetVehicleCapacity(math.min(efficiency, immediateEfficiency), prefabPrisonData.m_PrisonVanCapacity);
			int num2 = BuildingUtils.GetVehicleCapacity(immediateEfficiency, prefabPrisonData.m_PrisonVanCapacity);
			int availableVehicles = vehicleCapacity;
			int availableSpace = prefabPrisonData.m_PrisonerCapacity - occupants.Length;
			StackList<Entity> parkedVehicles = stackalloc Entity[vehicles.Length];
			if (resourceProductionBuffer.IsCreated)
			{
				float num3 = 0.0009765625f;
				num3 *= efficiency * (float)occupants.Length / (float)math.max(1, prefabPrisonData.m_PrisonerCapacity);
				for (int i = 0; i < resourceProductionBuffer.Length; i++)
				{
					ResourceProductionData resourceProductionData = resourceProductionBuffer[i];
					int resources2 = EconomyUtils.GetResources(resourceProductionData.m_Type, resources);
					int x = MathUtils.RoundToIntRandom(ref random, (float)resourceProductionData.m_ProductionRate * num3);
					x = math.max(0, math.min(x, resourceProductionData.m_StorageCapacity - resources2));
					EconomyUtils.AddResources(resourceProductionData.m_Type, x, resources);
				}
			}
			for (int j = 0; j < vehicles.Length; j++)
			{
				Entity vehicle = vehicles[j].m_Vehicle;
				if (!m_PublicTransportData.TryGetComponent(vehicle, out var componentData))
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
				PrefabRef prefabRef = m_PrefabRefData[vehicle];
				PublicTransportVehicleData publicTransportVehicleData = m_PrefabPublicTransportVehicleData[prefabRef.m_Prefab];
				availableVehicles--;
				availableSpace -= publicTransportVehicleData.m_PassengerCapacity;
				bool flag = --num2 < 0;
				if ((componentData.m_State & PublicTransportFlags.Disabled) != 0 != flag)
				{
					m_ActionQueue.Enqueue(PrisonAction.SetDisabled(vehicle, flag));
				}
			}
			int num4 = 0;
			while (num4 < dispatches.Length)
			{
				Entity request = dispatches[num4].m_Request;
				if (m_PrisonerTransportRequestData.HasComponent(request))
				{
					SpawnVehicle(jobIndex, ref random, entity, request, transform, ref prison, ref availableVehicles, ref availableSpace, ref parkedVehicles);
					dispatches.RemoveAt(num4);
				}
				else if (!m_ServiceRequestData.HasComponent(request))
				{
					dispatches.RemoveAt(num4);
				}
				else
				{
					num4++;
				}
			}
			while (parkedVehicles.Length > math.max(0, prefabPrisonData.m_PrisonVanCapacity + availableVehicles - vehicleCapacity))
			{
				int index = random.NextInt(parkedVehicles.Length);
				m_CommandBuffer.AddComponent<Deleted>(jobIndex, parkedVehicles[index]);
				parkedVehicles.RemoveAtSwapBack(index);
			}
			for (int k = 0; k < parkedVehicles.Length; k++)
			{
				Entity entity2 = parkedVehicles[k];
				Game.Vehicles.PublicTransport publicTransport = m_PublicTransportData[entity2];
				bool flag2 = availableVehicles <= 0 || availableSpace <= 0;
				if ((publicTransport.m_State & PublicTransportFlags.Disabled) != 0 != flag2)
				{
					m_ActionQueue.Enqueue(PrisonAction.SetDisabled(entity2, flag2));
				}
			}
			if (availableVehicles > 0)
			{
				prison.m_Flags |= PrisonFlags.HasAvailablePrisonVans;
				RequestTargetIfNeeded(jobIndex, entity, ref prison, availableVehicles);
			}
			else
			{
				prison.m_Flags &= ~PrisonFlags.HasAvailablePrisonVans;
			}
			if (availableSpace > 0)
			{
				prison.m_Flags |= PrisonFlags.HasPrisonerSpace;
			}
			else
			{
				prison.m_Flags &= ~PrisonFlags.HasPrisonerSpace;
			}
			prison.m_PrisonerWellbeing = (sbyte)math.clamp((int)math.round(efficiency * (float)prefabPrisonData.m_PrisonerWellbeing), -100, 100);
			prison.m_PrisonerHealth = (sbyte)math.clamp((int)math.round(efficiency * (float)prefabPrisonData.m_PrisonerHealth), -100, 100);
		}

		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Buildings.Prison prison, int availableVehicles)
		{
			if (!m_ServiceRequestData.HasComponent(prison.m_TargetRequest))
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_PrisonerTransportRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
				m_CommandBuffer.SetComponent(jobIndex, e, new PrisonerTransportRequest(entity, availableVehicles));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(16u));
			}
		}

		private void SpawnVehicle(int jobIndex, ref Random random, Entity entity, Entity request, Transform transform, ref Game.Buildings.Prison prison, ref int availableVehicles, ref int availableSpace, ref StackList<Entity> parkedVehicles)
		{
			if (!m_PrisonerTransportRequestData.TryGetComponent(request, out var componentData) || !m_EntityLookup.Exists(componentData.m_Target) || availableVehicles <= 0 || availableSpace <= 0)
			{
				return;
			}
			int2 passengerCapacity = new int2(1, availableSpace);
			int2 cargoCapacity = 0;
			Entity entity2 = Entity.Null;
			if (m_PathInformationData.TryGetComponent(request, out var componentData2) && componentData2.m_Origin != entity)
			{
				if (m_PrefabRefData.TryGetComponent(componentData2.m_Origin, out var componentData3) && m_PrefabPublicTransportVehicleData.TryGetComponent(componentData3.m_Prefab, out var componentData4))
				{
					passengerCapacity = componentData4.m_PassengerCapacity;
				}
				if (!CollectionUtils.RemoveValueSwapBack(ref parkedVehicles, componentData2.m_Origin))
				{
					return;
				}
				ParkedCar parkedCar = m_ParkedCarData[componentData2.m_Origin];
				entity2 = componentData2.m_Origin;
				m_CommandBuffer.RemoveComponent(jobIndex, entity2, in m_ParkedToMovingRemoveTypes);
				Game.Vehicles.CarLaneFlags flags = Game.Vehicles.CarLaneFlags.EndReached | Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.FixedLane;
				m_CommandBuffer.AddComponent(jobIndex, entity2, in m_ParkedToMovingCarAddTypes);
				m_CommandBuffer.SetComponent(jobIndex, entity2, new CarCurrentLane(parkedCar, flags));
				if (m_ParkingLaneData.HasComponent(parkedCar.m_Lane) || m_SpawnLocationData.HasComponent(parkedCar.m_Lane))
				{
					m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, parkedCar.m_Lane);
				}
			}
			if (entity2 == Entity.Null)
			{
				entity2 = m_TransportVehicleSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, transform, entity, default(NativeList<VehicleModel>), TransportType.Bus, EnergyTypes.FuelAndElectricity, SizeClass.Large, PublicTransportPurpose.PrisonerTransport, Resource.NoResource, ref passengerCapacity, ref cargoCapacity, parked: false);
				if (entity2 == Entity.Null)
				{
					return;
				}
				m_CommandBuffer.AddComponent(jobIndex, entity2, new Owner(entity));
			}
			availableVehicles--;
			availableSpace -= passengerCapacity.y;
			Game.Vehicles.PublicTransport component = default(Game.Vehicles.PublicTransport);
			component.m_State |= PublicTransportFlags.PrisonerTransport;
			component.m_RequestCount = 1;
			m_CommandBuffer.SetComponent(jobIndex, entity2, component);
			m_CommandBuffer.SetComponent(jobIndex, entity2, new Target(componentData.m_Target));
			m_CommandBuffer.SetBuffer<ServiceDispatch>(jobIndex, entity2).Add(new ServiceDispatch(request));
			if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
			{
				DynamicBuffer<PathElement> targetElements = m_CommandBuffer.SetBuffer<PathElement>(jobIndex, entity2);
				PathUtils.CopyPath(bufferData, default(PathOwner), 0, targetElements);
				m_CommandBuffer.SetComponent(jobIndex, entity2, new PathOwner(PathFlags.Updated));
				m_CommandBuffer.SetComponent(jobIndex, entity2, componentData2);
			}
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, entity2, completed: false));
			if (m_ServiceRequestData.HasComponent(prison.m_TargetRequest))
			{
				e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(prison.m_TargetRequest, Entity.Null, completed: true));
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct PrisonActionJob : IJob
	{
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		public NativeQueue<PrisonAction> m_ActionQueue;

		public void Execute()
		{
			PrisonAction item;
			while (m_ActionQueue.TryDequeue(out item))
			{
				if (m_PublicTransportData.TryGetComponent(item.m_Entity, out var componentData))
				{
					if (item.m_Disabled)
					{
						componentData.m_State |= PublicTransportFlags.AbandonRoute | PublicTransportFlags.Disabled;
					}
					else
					{
						componentData.m_State &= ~PublicTransportFlags.Disabled;
					}
					m_PublicTransportData[item.m_Entity] = componentData;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Buildings.Prison> __Game_Buildings_Prison_RW_ComponentTypeHandle;

		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;

		public BufferTypeHandle<Occupant> __Game_Buildings_Occupant_RW_BufferTypeHandle;

		public BufferTypeHandle<Resources> __Game_Economy_Resources_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<PrisonerTransportRequest> __Game_Simulation_PrisonerTransportRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PublicTransport> __Game_Vehicles_PublicTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrisonData> __Game_Prefabs_PrisonData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PublicTransportVehicleData> __Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ResourceProductionData> __Game_Prefabs_ResourceProductionData_RO_BufferLookup;

		public ComponentLookup<Game.Vehicles.PublicTransport> __Game_Vehicles_PublicTransport_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>(isReadOnly: true);
			__Game_Buildings_Prison_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Prison>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__Game_Buildings_Occupant_RW_BufferTypeHandle = state.GetBufferTypeHandle<Occupant>();
			__Game_Economy_Resources_RW_BufferTypeHandle = state.GetBufferTypeHandle<Resources>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Simulation_PrisonerTransportRequest_RO_ComponentLookup = state.GetComponentLookup<PrisonerTransportRequest>(isReadOnly: true);
			__Game_Simulation_ServiceRequest_RO_ComponentLookup = state.GetComponentLookup<ServiceRequest>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_PrisonData_RO_ComponentLookup = state.GetComponentLookup<PrisonData>(isReadOnly: true);
			__Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup = state.GetComponentLookup<PublicTransportVehicleData>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Prefabs_ResourceProductionData_RO_BufferLookup = state.GetBufferLookup<ResourceProductionData>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>();
		}
	}

	private EntityQuery m_BuildingQuery;

	private EntityQuery m_VehiclePrefabQuery;

	private EntityArchetype m_PrisonerTransportRequestArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private ComponentTypeSet m_ParkedToMovingRemoveTypes;

	private ComponentTypeSet m_ParkedToMovingCarAddTypes;

	private EndFrameBarrier m_EndFrameBarrier;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private TransportVehicleSelectData m_TransportVehicleSelectData;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 48;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_TransportVehicleSelectData = new TransportVehicleSelectData(this);
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.Prison>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_VehiclePrefabQuery = GetEntityQuery(TransportVehicleSelectData.GetEntityQueryDesc());
		m_PrisonerTransportRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<PrisonerTransportRequest>(), ComponentType.ReadWrite<RequestGroup>());
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
		RequireForUpdate(m_BuildingQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_TransportVehicleSelectData.PreUpdate(this, m_CityConfigurationSystem, m_VehiclePrefabQuery, Allocator.TempJob, out var jobHandle);
		NativeQueue<PrisonAction> actionQueue = new NativeQueue<PrisonAction>(Allocator.TempJob);
		PrisonTickJob jobData = new PrisonTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnedVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PrisonType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Prison_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_OccupantType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Occupant_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ResourcesType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Economy_Resources_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_PrisonerTransportRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PrisonerTransportRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrisonData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrisonData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPublicTransportVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourceProductionDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_ResourceProductionData_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_PrisonerTransportRequestArchetype = m_PrisonerTransportRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_ParkedToMovingRemoveTypes = m_ParkedToMovingRemoveTypes,
			m_ParkedToMovingCarAddTypes = m_ParkedToMovingCarAddTypes,
			m_TransportVehicleSelectData = m_TransportVehicleSelectData,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_ActionQueue = actionQueue.AsParallelWriter()
		};
		PrisonActionJob jobData2 = new PrisonActionJob
		{
			m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ActionQueue = actionQueue
		};
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData, m_BuildingQuery, JobHandle.CombineDependencies(base.Dependency, jobHandle));
		JobHandle jobHandle3 = IJobExtensions.Schedule(jobData2, jobHandle2);
		actionQueue.Dispose(jobHandle3);
		m_TransportVehicleSelectData.PostUpdate(jobHandle2);
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
	public PrisonAISystem()
	{
	}
}
