#define UNITY_ASSERTIONS
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Citizens;
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
public class PoliceStationAISystem : GameSystemBase
{
	private struct PoliceStationAction
	{
		public Entity m_Entity;

		public bool m_Disabled;

		public static PoliceStationAction SetDisabled(Entity vehicle, bool disabled)
		{
			return new PoliceStationAction
			{
				m_Entity = vehicle,
				m_Disabled = disabled
			};
		}
	}

	[BurstCompile]
	private struct PoliceStationTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicleType;

		public ComponentTypeHandle<Game.Buildings.PoliceStation> m_PoliceStationType;

		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		public BufferTypeHandle<Occupant> m_OccupantType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<PolicePatrolRequest> m_PolicePatrolRequestData;

		[ReadOnly]
		public ComponentLookup<PoliceEmergencyRequest> m_PoliceEmergencyRequestData;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> m_ServiceRequestData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Criminal> m_CriminalData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PoliceCar> m_PoliceCarData;

		[ReadOnly]
		public ComponentLookup<Helicopter> m_HelicopterData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<PoliceStationData> m_PrefabPoliceStationData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public uint m_SimulationFrameIndex;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_PrisonerTransportRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_PolicePatrolRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_PoliceEmergencyRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingCarAddTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingAircraftAddTypes;

		[ReadOnly]
		public PoliceCarSelectData m_PoliceCarSelectData;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<PoliceStationAction>.ParallelWriter m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Game.Buildings.PoliceStation> nativeArray3 = chunk.GetNativeArray(ref m_PoliceStationType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<OwnedVehicle> bufferAccessor3 = chunk.GetBufferAccessor(ref m_OwnedVehicleType);
			BufferAccessor<ServiceDispatch> bufferAccessor4 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			BufferAccessor<Occupant> bufferAccessor5 = chunk.GetBufferAccessor(ref m_OccupantType);
			bool outside = chunk.Has(ref m_OutsideConnectionType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				Game.Buildings.PoliceStation policeStation = nativeArray3[i];
				DynamicBuffer<OwnedVehicle> vehicles = bufferAccessor3[i];
				DynamicBuffer<ServiceDispatch> dispatches = bufferAccessor4[i];
				DynamicBuffer<Occupant> occupants = default(DynamicBuffer<Occupant>);
				if (bufferAccessor5.Length != 0)
				{
					occupants = bufferAccessor5[i];
				}
				PoliceStationData data = default(PoliceStationData);
				if (m_PrefabPoliceStationData.HasComponent(prefabRef.m_Prefab))
				{
					data = m_PrefabPoliceStationData[prefabRef.m_Prefab];
				}
				if (bufferAccessor2.Length != 0)
				{
					UpgradeUtils.CombineStats(ref data, bufferAccessor2[i], ref m_PrefabRefData, ref m_PrefabPoliceStationData);
				}
				float efficiency = BuildingUtils.GetEfficiency(bufferAccessor, i);
				float immediateEfficiency = BuildingUtils.GetImmediateEfficiency(bufferAccessor, i);
				Tick(unfilteredChunkIndex, entity, ref random, ref policeStation, data, vehicles, dispatches, occupants, efficiency, immediateEfficiency, outside);
				nativeArray3[i] = policeStation;
			}
		}

		private void Tick(int jobIndex, Entity entity, ref Random random, ref Game.Buildings.PoliceStation policeStation, PoliceStationData prefabPoliceStationData, DynamicBuffer<OwnedVehicle> vehicles, DynamicBuffer<ServiceDispatch> dispatches, DynamicBuffer<Occupant> occupants, float efficiency, float immediateEfficiency, bool outside)
		{
			int vehicleCapacity = BuildingUtils.GetVehicleCapacity(math.min(efficiency, immediateEfficiency), prefabPoliceStationData.m_PatrolCarCapacity);
			int vehicleCapacity2 = BuildingUtils.GetVehicleCapacity(math.min(efficiency, immediateEfficiency), prefabPoliceStationData.m_PoliceHelicopterCapacity);
			int num = BuildingUtils.GetVehicleCapacity(immediateEfficiency, prefabPoliceStationData.m_PatrolCarCapacity);
			int num2 = BuildingUtils.GetVehicleCapacity(immediateEfficiency, prefabPoliceStationData.m_PoliceHelicopterCapacity);
			int availableVehicles = vehicleCapacity;
			int availableVehicles2 = vehicleCapacity2;
			StackList<Entity> parkedVehicles = stackalloc Entity[vehicles.Length];
			StackList<Entity> parkedVehicles2 = stackalloc Entity[vehicles.Length];
			policeStation.m_PurposeMask = prefabPoliceStationData.m_PurposeMask;
			for (int i = 0; i < vehicles.Length; i++)
			{
				Entity vehicle = vehicles[i].m_Vehicle;
				if (!m_PoliceCarData.TryGetComponent(vehicle, out var componentData))
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
					flag2 = --num2 < 0;
				}
				else
				{
					availableVehicles--;
					flag2 = --num < 0;
				}
				if ((componentData.m_State & PoliceCarFlags.Disabled) != 0 != flag2)
				{
					m_ActionQueue.Enqueue(PoliceStationAction.SetDisabled(vehicle, flag2));
				}
			}
			int num3 = 0;
			while (num3 < dispatches.Length)
			{
				Entity request = dispatches[num3].m_Request;
				if (m_PolicePatrolRequestData.HasComponent(request) || m_PoliceEmergencyRequestData.HasComponent(request))
				{
					RoadTypes roadTypes = CheckPathType(request);
					switch (roadTypes)
					{
					case RoadTypes.Car:
						SpawnVehicle(jobIndex, ref random, entity, request, roadTypes, ref policeStation, ref availableVehicles, ref parkedVehicles, outside);
						break;
					case RoadTypes.Helicopter:
						SpawnVehicle(jobIndex, ref random, entity, request, roadTypes, ref policeStation, ref availableVehicles2, ref parkedVehicles2, outside);
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
			while (parkedVehicles.Length > math.max(0, prefabPoliceStationData.m_PatrolCarCapacity + availableVehicles - vehicleCapacity))
			{
				int index = random.NextInt(parkedVehicles.Length);
				m_CommandBuffer.AddComponent<Deleted>(jobIndex, parkedVehicles[index]);
				parkedVehicles.RemoveAtSwapBack(index);
			}
			while (parkedVehicles2.Length > math.max(0, prefabPoliceStationData.m_PoliceHelicopterCapacity + availableVehicles2 - vehicleCapacity2))
			{
				int index2 = random.NextInt(parkedVehicles2.Length);
				m_CommandBuffer.AddComponent<Deleted>(jobIndex, parkedVehicles2[index2]);
				parkedVehicles2.RemoveAtSwapBack(index2);
			}
			for (int j = 0; j < parkedVehicles.Length; j++)
			{
				Entity entity2 = parkedVehicles[j];
				Game.Vehicles.PoliceCar policeCar = m_PoliceCarData[entity2];
				bool flag3 = availableVehicles <= 0;
				if ((policeCar.m_State & PoliceCarFlags.Disabled) != 0 != flag3)
				{
					m_ActionQueue.Enqueue(PoliceStationAction.SetDisabled(entity2, flag3));
				}
			}
			for (int k = 0; k < parkedVehicles2.Length; k++)
			{
				Entity entity3 = parkedVehicles2[k];
				Game.Vehicles.PoliceCar policeCar2 = m_PoliceCarData[entity3];
				bool flag4 = availableVehicles2 <= 0;
				if ((policeCar2.m_State & PoliceCarFlags.Disabled) != 0 != flag4)
				{
					m_ActionQueue.Enqueue(PoliceStationAction.SetDisabled(entity3, flag4));
				}
			}
			if (availableVehicles > 0)
			{
				policeStation.m_Flags |= PoliceStationFlags.HasAvailablePatrolCars;
			}
			else
			{
				policeStation.m_Flags &= ~PoliceStationFlags.HasAvailablePatrolCars;
			}
			if (availableVehicles2 > 0)
			{
				policeStation.m_Flags |= PoliceStationFlags.HasAvailablePoliceHelicopters;
			}
			else
			{
				policeStation.m_Flags &= ~PoliceStationFlags.HasAvailablePoliceHelicopters;
			}
			int num4 = 0;
			if (occupants.IsCreated)
			{
				int num5 = 0;
				while (num5 < occupants.Length)
				{
					Entity occupant = occupants[num5].m_Occupant;
					if (!m_CriminalData.HasComponent(occupant))
					{
						occupants.RemoveAt(num5);
						continue;
					}
					Criminal criminal = m_CriminalData[occupant];
					if ((criminal.m_Flags & CriminalFlags.Arrested) == 0)
					{
						occupants.RemoveAt(num5);
						continue;
					}
					if ((criminal.m_Flags & CriminalFlags.Sentenced) != 0)
					{
						num4++;
					}
					num5++;
				}
			}
			if (num4 > 0)
			{
				policeStation.m_Flags |= PoliceStationFlags.NeedPrisonerTransport;
				RequestPrisonerTransport(jobIndex, entity, ref policeStation, num4);
			}
			else
			{
				policeStation.m_Flags &= ~PoliceStationFlags.NeedPrisonerTransport;
			}
			if (availableVehicles > 0 || availableVehicles2 > 0)
			{
				RequestTargetIfNeeded(jobIndex, entity, ref policeStation, availableVehicles, availableVehicles2);
			}
		}

		private void RequestPrisonerTransport(int jobIndex, Entity entity, ref Game.Buildings.PoliceStation policeStation, int priority)
		{
			if (!m_ServiceRequestData.HasComponent(policeStation.m_PrisonerTransportRequest))
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_PrisonerTransportRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new PrisonerTransportRequest(entity, priority));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(16u));
			}
		}

		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Buildings.PoliceStation policeStation, int availablePatrolCars, int availablePoliceHelicopters)
		{
			if (m_ServiceRequestData.HasComponent(policeStation.m_TargetRequest))
			{
				return;
			}
			if ((policeStation.m_PurposeMask & PolicePurpose.Patrol) != 0)
			{
				uint num = math.max(512u, 256u);
				if ((m_SimulationFrameIndex & (num - 1)) == 128)
				{
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_PolicePatrolRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e, new PolicePatrolRequest(entity, availablePatrolCars + availablePoliceHelicopters));
					m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
				}
			}
			else if ((policeStation.m_PurposeMask & (PolicePurpose.Emergency | PolicePurpose.Intelligence)) != 0 && availablePatrolCars > 0)
			{
				Entity e2 = m_CommandBuffer.CreateEntity(jobIndex, m_PoliceEmergencyRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e2, new ServiceRequest(reversed: true));
				m_CommandBuffer.SetComponent(jobIndex, e2, new PoliceEmergencyRequest(entity, Entity.Null, availablePatrolCars, policeStation.m_PurposeMask & (PolicePurpose.Emergency | PolicePurpose.Intelligence)));
				m_CommandBuffer.SetComponent(jobIndex, e2, new RequestGroup(4u));
			}
		}

		private void SpawnVehicle(int jobIndex, ref Random random, Entity entity, Entity request, RoadTypes roadType, ref Game.Buildings.PoliceStation policeStation, ref int availableVehicles, ref StackList<Entity> parkedVehicles, bool outside)
		{
			if (availableVehicles <= 0)
			{
				return;
			}
			PoliceCarFlags policeCarFlags = PoliceCarFlags.Empty;
			Entity entity2;
			PolicePurpose purposeMask;
			if (m_PolicePatrolRequestData.TryGetComponent(request, out var componentData))
			{
				entity2 = componentData.m_Target;
				purposeMask = policeStation.m_PurposeMask & PolicePurpose.Patrol;
			}
			else
			{
				if (!m_PoliceEmergencyRequestData.TryGetComponent(request, out var componentData2))
				{
					return;
				}
				entity2 = componentData2.m_Site;
				purposeMask = policeStation.m_PurposeMask & componentData2.m_Purpose;
				policeCarFlags |= PoliceCarFlags.AccidentTarget;
			}
			if (!m_EntityLookup.Exists(entity2))
			{
				return;
			}
			Entity entity3 = Entity.Null;
			if (m_PathInformationData.TryGetComponent(request, out var componentData3) && componentData3.m_Origin != entity)
			{
				if (!CollectionUtils.RemoveValueSwapBack(ref parkedVehicles, componentData3.m_Origin))
				{
					return;
				}
				ParkedCar parkedCar = m_ParkedCarData[componentData3.m_Origin];
				purposeMask = m_PoliceCarData[componentData3.m_Origin].m_PurposeMask;
				entity3 = componentData3.m_Origin;
				m_CommandBuffer.RemoveComponent(jobIndex, entity3, in m_ParkedToMovingRemoveTypes);
				switch (roadType)
				{
				case RoadTypes.Car:
				{
					Game.Vehicles.CarLaneFlags flags2 = Game.Vehicles.CarLaneFlags.EndReached | Game.Vehicles.CarLaneFlags.ParkingSpace | Game.Vehicles.CarLaneFlags.FixedLane;
					m_CommandBuffer.AddComponent(jobIndex, entity3, in m_ParkedToMovingCarAddTypes);
					m_CommandBuffer.SetComponent(jobIndex, entity3, new CarCurrentLane(parkedCar, flags2));
					break;
				}
				case RoadTypes.Helicopter:
				{
					AircraftLaneFlags flags = AircraftLaneFlags.EndReached | AircraftLaneFlags.TransformTarget | AircraftLaneFlags.ParkingSpace;
					m_CommandBuffer.AddComponent(jobIndex, entity3, in m_ParkedToMovingAircraftAddTypes);
					m_CommandBuffer.SetComponent(jobIndex, entity3, new AircraftCurrentLane(parkedCar, flags));
					break;
				}
				}
				if (m_ParkingLaneData.HasComponent(parkedCar.m_Lane) || m_SpawnLocationData.HasComponent(parkedCar.m_Lane))
				{
					m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, parkedCar.m_Lane);
				}
			}
			if (entity3 == Entity.Null)
			{
				entity3 = m_PoliceCarSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, m_TransformData[entity], entity, Entity.Null, ref purposeMask, roadType, parked: false);
				if (entity3 == Entity.Null)
				{
					return;
				}
				m_CommandBuffer.AddComponent(jobIndex, entity3, new Owner(entity));
			}
			availableVehicles--;
			m_CommandBuffer.SetComponent(jobIndex, entity3, new Game.Vehicles.PoliceCar(policeCarFlags, 1, policeStation.m_PurposeMask & purposeMask));
			m_CommandBuffer.SetComponent(jobIndex, entity3, new Target(entity2));
			m_CommandBuffer.SetBuffer<ServiceDispatch>(jobIndex, entity3).Add(new ServiceDispatch(request));
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, entity3, completed: false));
			if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
			{
				DynamicBuffer<PathElement> targetElements = m_CommandBuffer.SetBuffer<PathElement>(jobIndex, entity3);
				PathUtils.CopyPath(bufferData, default(PathOwner), 0, targetElements);
				m_CommandBuffer.SetComponent(jobIndex, entity3, new PathOwner(PathFlags.Updated));
				m_CommandBuffer.SetComponent(jobIndex, entity3, componentData3);
			}
			if (m_ServiceRequestData.HasComponent(policeStation.m_TargetRequest))
			{
				e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(policeStation.m_TargetRequest, Entity.Null, completed: true));
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
	private struct PoliceStationActionJob : IJob
	{
		public ComponentLookup<Game.Vehicles.PoliceCar> m_PoliceCarData;

		public NativeQueue<PoliceStationAction> m_ActionQueue;

		public void Execute()
		{
			PoliceStationAction item;
			while (m_ActionQueue.TryDequeue(out item))
			{
				if (m_PoliceCarData.TryGetComponent(item.m_Entity, out var componentData))
				{
					if (item.m_Disabled)
					{
						componentData.m_State |= PoliceCarFlags.Disabled;
					}
					else
					{
						componentData.m_State &= ~PoliceCarFlags.Disabled;
					}
					m_PoliceCarData[item.m_Entity] = componentData;
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
		public ComponentTypeHandle<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Buildings.PoliceStation> __Game_Buildings_PoliceStation_RW_ComponentTypeHandle;

		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;

		public BufferTypeHandle<Occupant> __Game_Buildings_Occupant_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<PolicePatrolRequest> __Game_Simulation_PolicePatrolRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PoliceEmergencyRequest> __Game_Simulation_PoliceEmergencyRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Criminal> __Game_Citizens_Criminal_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.PoliceCar> __Game_Vehicles_PoliceCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Helicopter> __Game_Vehicles_Helicopter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PoliceStationData> __Game_Prefabs_PoliceStationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		public ComponentLookup<Game.Vehicles.PoliceCar> __Game_Vehicles_PoliceCar_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_Efficiency_RO_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>(isReadOnly: true);
			__Game_Buildings_PoliceStation_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.PoliceStation>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__Game_Buildings_Occupant_RW_BufferTypeHandle = state.GetBufferTypeHandle<Occupant>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Simulation_PolicePatrolRequest_RO_ComponentLookup = state.GetComponentLookup<PolicePatrolRequest>(isReadOnly: true);
			__Game_Simulation_PoliceEmergencyRequest_RO_ComponentLookup = state.GetComponentLookup<PoliceEmergencyRequest>(isReadOnly: true);
			__Game_Simulation_ServiceRequest_RO_ComponentLookup = state.GetComponentLookup<ServiceRequest>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Citizens_Criminal_RO_ComponentLookup = state.GetComponentLookup<Criminal>(isReadOnly: true);
			__Game_Vehicles_PoliceCar_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PoliceCar>(isReadOnly: true);
			__Game_Vehicles_Helicopter_RO_ComponentLookup = state.GetComponentLookup<Helicopter>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_PoliceStationData_RO_ComponentLookup = state.GetComponentLookup<PoliceStationData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Vehicles_PoliceCar_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PoliceCar>();
		}
	}

	private EntityQuery m_BuildingQuery;

	private EntityQuery m_VehiclePrefabQuery;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private PoliceCarSelectData m_PoliceCarSelectData;

	private EntityArchetype m_PrisonerTransportRequestArchetype;

	private EntityArchetype m_PolicePatrolRequestArchetype;

	private EntityArchetype m_PoliceEmergencyRequestArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private ComponentTypeSet m_ParkedToMovingRemoveTypes;

	private ComponentTypeSet m_ParkedToMovingCarAddTypes;

	private ComponentTypeSet m_ParkedToMovingAircraftAddTypes;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 128;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_PoliceCarSelectData = new PoliceCarSelectData(this);
		m_BuildingQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Game.Buildings.PoliceStation>(),
				ComponentType.ReadOnly<ServiceDispatch>(),
				ComponentType.ReadOnly<PrefabRef>()
			},
			Any = new ComponentType[2]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Game.Objects.OutsideConnection>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Temp>(),
				ComponentType.ReadOnly<Deleted>()
			}
		});
		m_VehiclePrefabQuery = GetEntityQuery(PoliceCarSelectData.GetEntityQueryDesc());
		m_PrisonerTransportRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<PrisonerTransportRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_PolicePatrolRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<PolicePatrolRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_PoliceEmergencyRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<PoliceEmergencyRequest>(), ComponentType.ReadWrite<RequestGroup>());
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
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_PoliceCarSelectData.PreUpdate(this, m_CityConfigurationSystem, m_VehiclePrefabQuery, Allocator.TempJob, out var jobHandle);
		NativeQueue<PoliceStationAction> actionQueue = new NativeQueue<PoliceStationAction>(Allocator.TempJob);
		PoliceStationTickJob jobData = new PoliceStationTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnedVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PoliceStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_PoliceStation_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_OccupantType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Occupant_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_PolicePatrolRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PolicePatrolRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PoliceEmergencyRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PoliceEmergencyRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CriminalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Criminal_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PoliceCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PoliceCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HelicopterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Helicopter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPoliceStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PoliceStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_SimulationFrameIndex = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_PrisonerTransportRequestArchetype = m_PrisonerTransportRequestArchetype,
			m_PolicePatrolRequestArchetype = m_PolicePatrolRequestArchetype,
			m_PoliceEmergencyRequestArchetype = m_PoliceEmergencyRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_ParkedToMovingRemoveTypes = m_ParkedToMovingRemoveTypes,
			m_ParkedToMovingCarAddTypes = m_ParkedToMovingCarAddTypes,
			m_ParkedToMovingAircraftAddTypes = m_ParkedToMovingAircraftAddTypes,
			m_PoliceCarSelectData = m_PoliceCarSelectData,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_ActionQueue = actionQueue.AsParallelWriter()
		};
		PoliceStationActionJob jobData2 = new PoliceStationActionJob
		{
			m_PoliceCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PoliceCar_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ActionQueue = actionQueue
		};
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData, m_BuildingQuery, JobHandle.CombineDependencies(base.Dependency, jobHandle));
		JobHandle jobHandle3 = IJobExtensions.Schedule(jobData2, jobHandle2);
		actionQueue.Dispose(jobHandle3);
		m_PoliceCarSelectData.PostUpdate(jobHandle2);
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
	public PoliceStationAISystem()
	{
	}
}
