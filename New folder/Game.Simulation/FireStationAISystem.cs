#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Net;
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
public class FireStationAISystem : GameSystemBase
{
	private struct FireStationAction
	{
		public Entity m_Entity;

		public bool m_Disabled;

		public bool m_DisasterResponse;

		public static FireStationAction SetFlags(Entity vehicle, bool disabled, bool disasterResponse)
		{
			return new FireStationAction
			{
				m_Entity = vehicle,
				m_Disabled = disabled,
				m_DisasterResponse = disasterResponse
			};
		}
	}

	[BurstCompile]
	private struct FireStationTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicleType;

		public ComponentTypeHandle<Game.Buildings.FireStation> m_FireStationType;

		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> m_FireRescueRequestData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> m_ServiceRequestData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.FireEngine> m_FireEngineData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingsData;

		[ReadOnly]
		public ComponentLookup<Helicopter> m_HelicopterData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<FireStationData> m_PrefabFireStationData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_FireRescueRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingCarAddTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingAircraftAddTypes;

		[ReadOnly]
		public FireEngineSelectData m_FireEngineSelectData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<FireStationAction>.ParallelWriter m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Game.Buildings.FireStation> nativeArray3 = chunk.GetNativeArray(ref m_FireStationType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<OwnedVehicle> bufferAccessor3 = chunk.GetBufferAccessor(ref m_OwnedVehicleType);
			BufferAccessor<ServiceDispatch> bufferAccessor4 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				Game.Buildings.FireStation fireStation = nativeArray3[i];
				DynamicBuffer<OwnedVehicle> vehicles = bufferAccessor3[i];
				DynamicBuffer<ServiceDispatch> dispatches = bufferAccessor4[i];
				FireStationData data = default(FireStationData);
				if (m_PrefabFireStationData.HasComponent(prefabRef.m_Prefab))
				{
					data = m_PrefabFireStationData[prefabRef.m_Prefab];
				}
				if (bufferAccessor2.Length != 0)
				{
					UpgradeUtils.CombineStats(ref data, bufferAccessor2[i], ref m_PrefabRefData, ref m_PrefabFireStationData);
				}
				float efficiency = BuildingUtils.GetEfficiency(bufferAccessor, i);
				float immediateEfficiency = BuildingUtils.GetImmediateEfficiency(bufferAccessor, i);
				Tick(unfilteredChunkIndex, entity, ref random, ref fireStation, data, vehicles, dispatches, efficiency, immediateEfficiency);
				nativeArray3[i] = fireStation;
			}
		}

		private void Tick(int jobIndex, Entity entity, ref Random random, ref Game.Buildings.FireStation fireStation, FireStationData prefabFireStationData, DynamicBuffer<OwnedVehicle> vehicles, DynamicBuffer<ServiceDispatch> dispatches, float efficiency, float immediateEfficiency)
		{
			int vehicleCapacity = BuildingUtils.GetVehicleCapacity(math.min(efficiency, immediateEfficiency), prefabFireStationData.m_FireEngineCapacity);
			int vehicleCapacity2 = BuildingUtils.GetVehicleCapacity(math.min(efficiency, immediateEfficiency), prefabFireStationData.m_FireHelicopterCapacity);
			int num = BuildingUtils.GetVehicleCapacity(immediateEfficiency, prefabFireStationData.m_FireEngineCapacity);
			int num2 = BuildingUtils.GetVehicleCapacity(immediateEfficiency, prefabFireStationData.m_FireHelicopterCapacity);
			int availableVehicles = vehicleCapacity;
			int availableVehicles2 = vehicleCapacity2;
			int freeVehicles = prefabFireStationData.m_FireEngineCapacity;
			int freeVehicles2 = prefabFireStationData.m_FireHelicopterCapacity;
			StackList<Entity> parkedVehicles = stackalloc Entity[vehicles.Length];
			StackList<Entity> parkedVehicles2 = stackalloc Entity[vehicles.Length];
			int disasterResponseAvailable = BuildingUtils.GetVehicleCapacity(math.min(efficiency, immediateEfficiency), prefabFireStationData.m_DisasterResponseCapacity);
			float efficiency2 = prefabFireStationData.m_VehicleEfficiency * (0.5f + efficiency * 0.5f);
			for (int i = 0; i < vehicles.Length; i++)
			{
				Entity vehicle = vehicles[i].m_Vehicle;
				if (!m_FireEngineData.TryGetComponent(vehicle, out var componentData))
				{
					continue;
				}
				bool flag = m_HelicopterData.HasComponent(vehicle);
				if (m_ParkedCarData.TryGetComponent(vehicle, out var componentData2))
				{
					if (!m_EntityLookup.Exists(componentData2.m_Lane))
					{
						m_CommandBuffer.AddComponent<Deleted>(jobIndex, vehicle);
					}
					else if (flag)
					{
						parkedVehicles2.AddNoResize(vehicle);
					}
					else
					{
						parkedVehicles.AddNoResize(vehicle);
					}
					continue;
				}
				bool flag2;
				if (flag)
				{
					availableVehicles2--;
					freeVehicles2--;
					flag2 = --num2 < 0;
				}
				else
				{
					availableVehicles--;
					freeVehicles--;
					flag2 = --num < 0;
				}
				bool flag3 = (componentData.m_State & FireEngineFlags.DisasterResponse) != 0;
				if (flag3)
				{
					disasterResponseAvailable--;
				}
				if ((componentData.m_State & FireEngineFlags.Disabled) != 0 != flag2)
				{
					m_ActionQueue.Enqueue(FireStationAction.SetFlags(vehicle, flag2, flag3));
				}
			}
			if (m_BuildingsData.TryGetComponent(entity, out var componentData3) && BuildingUtils.CheckOption(componentData3, BuildingOption.Inactive))
			{
				dispatches.Clear();
			}
			else
			{
				int num3 = 0;
				while (num3 < dispatches.Length)
				{
					Entity request = dispatches[num3].m_Request;
					if (m_FireRescueRequestData.HasComponent(request))
					{
						RoadTypes roadTypes = CheckPathType(request);
						switch (roadTypes)
						{
						case RoadTypes.Car:
							SpawnVehicle(jobIndex, ref random, entity, request, roadTypes, efficiency2, ref fireStation, ref availableVehicles, ref freeVehicles, ref disasterResponseAvailable, ref parkedVehicles);
							break;
						case RoadTypes.Helicopter:
							SpawnVehicle(jobIndex, ref random, entity, request, roadTypes, efficiency2, ref fireStation, ref availableVehicles2, ref freeVehicles2, ref disasterResponseAvailable, ref parkedVehicles2);
							break;
						}
						dispatches.RemoveAt(num3);
					}
					else if (!m_ServiceRequestData.HasComponent(request))
					{
						dispatches.RemoveAt(num3);
					}
					else
					{
						num3++;
					}
				}
			}
			while (parkedVehicles.Length > math.max(0, prefabFireStationData.m_FireEngineCapacity + availableVehicles - vehicleCapacity))
			{
				int index = random.NextInt(parkedVehicles.Length);
				m_CommandBuffer.AddComponent<Deleted>(jobIndex, parkedVehicles[index]);
				parkedVehicles.RemoveAtSwapBack(index);
			}
			while (parkedVehicles2.Length > math.max(0, prefabFireStationData.m_FireHelicopterCapacity + availableVehicles2 - vehicleCapacity2))
			{
				int index2 = random.NextInt(parkedVehicles2.Length);
				m_CommandBuffer.AddComponent<Deleted>(jobIndex, parkedVehicles2[index2]);
				parkedVehicles2.RemoveAtSwapBack(index2);
			}
			for (int j = 0; j < parkedVehicles.Length; j++)
			{
				Entity entity2 = parkedVehicles[j];
				Game.Vehicles.FireEngine fireEngine = m_FireEngineData[entity2];
				bool flag4 = availableVehicles <= 0;
				bool flag5 = disasterResponseAvailable > 0;
				if ((fireEngine.m_State & FireEngineFlags.Disabled) != 0 != flag4 || (fireEngine.m_State & FireEngineFlags.DisasterResponse) != 0 != flag5)
				{
					m_ActionQueue.Enqueue(FireStationAction.SetFlags(entity2, flag4, flag5));
				}
			}
			for (int k = 0; k < parkedVehicles2.Length; k++)
			{
				Entity entity3 = parkedVehicles2[k];
				Game.Vehicles.FireEngine fireEngine2 = m_FireEngineData[entity3];
				bool flag6 = availableVehicles2 <= 0;
				bool flag7 = disasterResponseAvailable > 0;
				if ((fireEngine2.m_State & FireEngineFlags.Disabled) != 0 != flag6 || (fireEngine2.m_State & FireEngineFlags.DisasterResponse) != 0 != flag7)
				{
					m_ActionQueue.Enqueue(FireStationAction.SetFlags(entity3, flag6, flag7));
				}
			}
			if (availableVehicles > 0)
			{
				fireStation.m_Flags |= FireStationFlags.HasAvailableFireEngines;
			}
			else
			{
				fireStation.m_Flags &= ~FireStationFlags.HasAvailableFireEngines;
			}
			if (freeVehicles > 0)
			{
				fireStation.m_Flags |= FireStationFlags.HasFreeFireEngines;
			}
			else
			{
				fireStation.m_Flags &= ~FireStationFlags.HasFreeFireEngines;
			}
			if (availableVehicles2 > 0)
			{
				fireStation.m_Flags |= FireStationFlags.HasAvailableFireHelicopters;
			}
			else
			{
				fireStation.m_Flags &= ~FireStationFlags.HasAvailableFireHelicopters;
			}
			if (freeVehicles2 > 0)
			{
				fireStation.m_Flags |= FireStationFlags.HasFreeFireHelicopters;
			}
			else
			{
				fireStation.m_Flags &= ~FireStationFlags.HasFreeFireHelicopters;
			}
			if (disasterResponseAvailable > 0)
			{
				fireStation.m_Flags |= FireStationFlags.DisasterResponseAvailable;
			}
			else
			{
				fireStation.m_Flags &= ~FireStationFlags.DisasterResponseAvailable;
			}
			if (availableVehicles > 0 || availableVehicles2 > 0)
			{
				RequestTargetIfNeeded(jobIndex, entity, ref fireStation, availableVehicles, availableVehicles2);
			}
		}

		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Buildings.FireStation fireStation, int availableFireEngines, int availableFireHelicopters)
		{
			if (!m_ServiceRequestData.HasComponent(fireStation.m_TargetRequest))
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_FireRescueRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
				m_CommandBuffer.SetComponent(jobIndex, e, new FireRescueRequest(entity, availableFireEngines + availableFireHelicopters, FireRescueRequestType.Fire));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(4u));
			}
		}

		private void SpawnVehicle(int jobIndex, ref Random random, Entity entity, Entity request, RoadTypes roadType, float efficiency, ref Game.Buildings.FireStation fireStation, ref int availableVehicles, ref int freeVehicles, ref int disasterResponseAvailable, ref StackList<Entity> parkedVehicles)
		{
			if (!m_FireRescueRequestData.TryGetComponent(request, out var componentData) || !m_EntityLookup.Exists(componentData.m_Target) || math.select(availableVehicles, freeVehicles, componentData.m_Target == entity) <= 0 || (componentData.m_Type == FireRescueRequestType.Disaster && disasterResponseAvailable <= 0))
			{
				return;
			}
			float2 extinguishingCapacity = new float2(float.Epsilon, float.MaxValue);
			Entity entity2 = Entity.Null;
			if (m_PathInformationData.TryGetComponent(request, out var componentData2) && componentData2.m_Origin != entity)
			{
				if (!CollectionUtils.RemoveValueSwapBack(ref parkedVehicles, componentData2.m_Origin))
				{
					return;
				}
				ParkedCar parkedCar = m_ParkedCarData[componentData2.m_Origin];
				extinguishingCapacity = m_FireEngineData[componentData2.m_Origin].m_ExtinguishingAmount;
				entity2 = componentData2.m_Origin;
				m_CommandBuffer.RemoveComponent(jobIndex, entity2, in m_ParkedToMovingRemoveTypes);
				switch (roadType)
				{
				case RoadTypes.Car:
				{
					Game.Vehicles.CarLaneFlags flags2 = Game.Vehicles.CarLaneFlags.EndReached | Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.FixedLane;
					m_CommandBuffer.AddComponent(jobIndex, entity2, in m_ParkedToMovingCarAddTypes);
					m_CommandBuffer.SetComponent(jobIndex, entity2, new CarCurrentLane(parkedCar, flags2));
					break;
				}
				case RoadTypes.Helicopter:
				{
					AircraftLaneFlags flags = AircraftLaneFlags.EndReached | AircraftLaneFlags.TransformTarget | AircraftLaneFlags.ParkingSpace;
					m_CommandBuffer.AddComponent(jobIndex, entity2, in m_ParkedToMovingAircraftAddTypes);
					m_CommandBuffer.SetComponent(jobIndex, entity2, new AircraftCurrentLane(parkedCar, flags));
					break;
				}
				}
				if (m_ParkingLaneData.HasComponent(parkedCar.m_Lane) || m_SpawnLocationData.HasComponent(parkedCar.m_Lane))
				{
					m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, parkedCar.m_Lane);
				}
			}
			if (entity2 == Entity.Null)
			{
				entity2 = m_FireEngineSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, m_TransformData[entity], entity, Entity.Null, ref extinguishingCapacity, roadType, parked: false);
				if (entity2 == Entity.Null)
				{
					return;
				}
				m_CommandBuffer.AddComponent(jobIndex, entity2, new Owner(entity));
			}
			FireEngineFlags fireEngineFlags = (FireEngineFlags)0u;
			if (componentData.m_Type == FireRescueRequestType.Disaster)
			{
				fireEngineFlags |= FireEngineFlags.DisasterResponse;
				disasterResponseAvailable--;
			}
			freeVehicles--;
			availableVehicles--;
			m_CommandBuffer.SetComponent(jobIndex, entity2, new Game.Vehicles.FireEngine(fireEngineFlags, 1, extinguishingCapacity.y, efficiency));
			m_CommandBuffer.SetComponent(jobIndex, entity2, new Target(componentData.m_Target));
			m_CommandBuffer.SetBuffer<ServiceDispatch>(jobIndex, entity2).Add(new ServiceDispatch(request));
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, entity2, completed: false));
			if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
			{
				DynamicBuffer<PathElement> targetElements = m_CommandBuffer.SetBuffer<PathElement>(jobIndex, entity2);
				PathUtils.CopyPath(bufferData, default(PathOwner), 0, targetElements);
				m_CommandBuffer.SetComponent(jobIndex, entity2, new PathOwner(PathFlags.Updated));
				m_CommandBuffer.SetComponent(jobIndex, entity2, componentData2);
			}
			if (m_ServiceRequestData.HasComponent(fireStation.m_TargetRequest))
			{
				e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(fireStation.m_TargetRequest, Entity.Null, completed: true));
			}
		}

		private RoadTypes CheckPathType(Entity request)
		{
			if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length >= 1)
			{
				PathElement pathElement = bufferData[0];
				if (m_PrefabRefData.TryGetComponent(pathElement.m_Target, out var componentData) && m_PrefabSpawnLocationData.TryGetComponent(componentData.m_Prefab, out var componentData2))
				{
					return componentData2.m_RoadTypes;
				}
			}
			return RoadTypes.Car;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FireStationActionJob : IJob
	{
		public ComponentLookup<Game.Vehicles.FireEngine> m_FireEngineData;

		public NativeQueue<FireStationAction> m_ActionQueue;

		public void Execute()
		{
			FireStationAction item;
			while (m_ActionQueue.TryDequeue(out item))
			{
				if (m_FireEngineData.TryGetComponent(item.m_Entity, out var componentData))
				{
					if (item.m_Disabled)
					{
						componentData.m_State |= FireEngineFlags.Disabled;
					}
					else
					{
						componentData.m_State &= ~FireEngineFlags.Disabled;
					}
					if (item.m_DisasterResponse)
					{
						componentData.m_State |= FireEngineFlags.DisasterResponse;
					}
					else
					{
						componentData.m_State &= ~FireEngineFlags.DisasterResponse;
					}
					m_FireEngineData[item.m_Entity] = componentData;
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Buildings.FireStation> __Game_Buildings_FireStation_RW_ComponentTypeHandle;

		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> __Game_Simulation_FireRescueRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.FireEngine> __Game_Vehicles_FireEngine_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Helicopter> __Game_Vehicles_Helicopter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<FireStationData> __Game_Prefabs_FireStationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		public ComponentLookup<Game.Vehicles.FireEngine> __Game_Vehicles_FireEngine_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>(isReadOnly: true);
			__Game_Buildings_FireStation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.FireStation>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Simulation_FireRescueRequest_RO_ComponentLookup = state.GetComponentLookup<FireRescueRequest>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Simulation_ServiceRequest_RO_ComponentLookup = state.GetComponentLookup<ServiceRequest>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Vehicles_FireEngine_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.FireEngine>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Vehicles_Helicopter_RO_ComponentLookup = state.GetComponentLookup<Helicopter>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_FireStationData_RO_ComponentLookup = state.GetComponentLookup<FireStationData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Vehicles_FireEngine_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.FireEngine>();
		}
	}

	private EntityQuery m_BuildingQuery;

	private EntityQuery m_VehiclePrefabQuery;

	private EntityArchetype m_FireRescueRequestArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private ComponentTypeSet m_ParkedToMovingRemoveTypes;

	private ComponentTypeSet m_ParkedToMovingCarAddTypes;

	private ComponentTypeSet m_ParkedToMovingAircraftAddTypes;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EndFrameBarrier m_EndFrameBarrier;

	private FireEngineSelectData m_FireEngineSelectData;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 112;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_FireEngineSelectData = new FireEngineSelectData(this);
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.FireStation>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_VehiclePrefabQuery = GetEntityQuery(FireEngineSelectData.GetEntityQueryDesc());
		m_FireRescueRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<FireRescueRequest>(), ComponentType.ReadWrite<RequestGroup>());
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
		m_ParkedToMovingAircraftAddTypes = new ComponentTypeSet(new ComponentType[13]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<AircraftNavigation>(),
			ComponentType.ReadWrite<AircraftNavigationLane>(),
			ComponentType.ReadWrite<AircraftCurrentLane>(),
			ComponentType.ReadWrite<PathOwner>(),
			ComponentType.ReadWrite<Target>(),
			ComponentType.ReadWrite<Blocker>(),
			ComponentType.ReadWrite<PathElement>(),
			ComponentType.ReadWrite<PathInformation>(),
			ComponentType.ReadWrite<ServiceDispatch>(),
			ComponentType.ReadWrite<Updated>()
		});
		RequireForUpdate(m_BuildingQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_FireEngineSelectData.PreUpdate(this, m_CityConfigurationSystem, m_VehiclePrefabQuery, Allocator.TempJob, out var jobHandle);
		NativeQueue<FireStationAction> actionQueue = new NativeQueue<FireStationAction>(Allocator.TempJob);
		FireStationTickJob jobData = new FireStationTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnedVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_FireStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_FireStation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_FireRescueRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_FireRescueRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FireEngineData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_FireEngine_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingsData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HelicopterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Helicopter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabFireStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_FireStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_FireRescueRequestArchetype = m_FireRescueRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_ParkedToMovingRemoveTypes = m_ParkedToMovingRemoveTypes,
			m_ParkedToMovingCarAddTypes = m_ParkedToMovingCarAddTypes,
			m_ParkedToMovingAircraftAddTypes = m_ParkedToMovingAircraftAddTypes,
			m_FireEngineSelectData = m_FireEngineSelectData,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_ActionQueue = actionQueue.AsParallelWriter()
		};
		FireStationActionJob jobData2 = new FireStationActionJob
		{
			m_FireEngineData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_FireEngine_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ActionQueue = actionQueue
		};
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData, m_BuildingQuery, JobHandle.CombineDependencies(base.Dependency, jobHandle));
		JobHandle jobHandle3 = IJobExtensions.Schedule(jobData2, jobHandle2);
		actionQueue.Dispose(jobHandle3);
		m_FireEngineSelectData.PostUpdate(jobHandle2);
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
	public FireStationAISystem()
	{
	}
}
