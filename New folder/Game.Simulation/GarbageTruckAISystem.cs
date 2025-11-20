using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Areas;
using Game.Buildings;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Rendering;
using Game.Tools;
using Game.Vehicles;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class GarbageTruckAISystem : GameSystemBase
{
	private enum GarbageActionType
	{
		Collect,
		Unload,
		AddRequest,
		ClearLane,
		BumpDispatchIndex
	}

	private struct GarbageAction
	{
		public Entity m_Vehicle;

		public Entity m_Target;

		public Entity m_Request;

		public GarbageActionType m_Type;

		public int m_Capacity;

		public int m_MaxAmount;
	}

	[BurstCompile]
	private struct GarbageTruckTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		public ComponentTypeHandle<Game.Vehicles.GarbageTruck> m_GarbageTruckType;

		public ComponentTypeHandle<Car> m_CarType;

		public ComponentTypeHandle<CarCurrentLane> m_CurrentLaneType;

		public ComponentTypeHandle<Target> m_TargetType;

		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		public BufferTypeHandle<CarNavigationLane> m_CarNavigationLaneType;

		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<Quantity> m_QuantityData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Unspawned> m_UnspawnedData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<GarageLane> m_GarageLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public ComponentLookup<GarbageTruckData> m_PrefabGarbageTruckData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> m_PrefabSpawnableBuildingData;

		[ReadOnly]
		public ComponentLookup<ZoneData> m_PrefabZoneData;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public ComponentLookup<GarbageCollectionRequest> m_GarbageCollectionRequestData;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> m_GarbageProducerData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.GarbageFacility> m_GarbageFacilityData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> m_SubObjects;

		[ReadOnly]
		public BufferLookup<ConnectedBuilding> m_ConnectedBuildings;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public BufferLookup<LaneOverlap> m_LaneOverlaps;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> m_ServiceDistricts;

		[NativeDisableParallelForRestriction]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public uint m_SimulationFrameIndex;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_GarbageCollectionRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedCarRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedAddTypes;

		[ReadOnly]
		public GarbageParameterData m_GarbageParameters;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public NativeQueue<GarbageAction>.ParallelWriter m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<PathInformation> nativeArray4 = chunk.GetNativeArray(ref m_PathInformationType);
			NativeArray<CarCurrentLane> nativeArray5 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<Game.Vehicles.GarbageTruck> nativeArray6 = chunk.GetNativeArray(ref m_GarbageTruckType);
			NativeArray<Car> nativeArray7 = chunk.GetNativeArray(ref m_CarType);
			NativeArray<Target> nativeArray8 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<PathOwner> nativeArray9 = chunk.GetNativeArray(ref m_PathOwnerType);
			BufferAccessor<CarNavigationLane> bufferAccessor = chunk.GetBufferAccessor(ref m_CarNavigationLaneType);
			BufferAccessor<ServiceDispatch> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Owner owner = nativeArray2[i];
				PrefabRef prefabRef = nativeArray3[i];
				PathInformation pathInformation = nativeArray4[i];
				Game.Vehicles.GarbageTruck garbageTruck = nativeArray6[i];
				Car car = nativeArray7[i];
				CarCurrentLane currentLane = nativeArray5[i];
				PathOwner pathOwner = nativeArray9[i];
				Target target = nativeArray8[i];
				DynamicBuffer<CarNavigationLane> navigationLanes = bufferAccessor[i];
				DynamicBuffer<ServiceDispatch> serviceDispatches = bufferAccessor2[i];
				VehicleUtils.CheckUnspawned(unfilteredChunkIndex, entity, currentLane, isUnspawned, m_CommandBuffer);
				Tick(unfilteredChunkIndex, entity, owner, prefabRef, pathInformation, navigationLanes, serviceDispatches, ref random, ref garbageTruck, ref car, ref currentLane, ref pathOwner, ref target);
				nativeArray6[i] = garbageTruck;
				nativeArray7[i] = car;
				nativeArray5[i] = currentLane;
				nativeArray9[i] = pathOwner;
				nativeArray8[i] = target;
			}
		}

		private void Tick(int jobIndex, Entity vehicleEntity, Owner owner, PrefabRef prefabRef, PathInformation pathInformation, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Unity.Mathematics.Random random, ref Game.Vehicles.GarbageTruck garbageTruck, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if (VehicleUtils.ResetUpdatedPath(ref pathOwner))
			{
				ResetPath(jobIndex, vehicleEntity, pathInformation, serviceDispatches, owner, ref random, ref garbageTruck, ref car, ref currentLane, ref pathOwner);
			}
			GarbageTruckData prefabGarbageTruckData = m_PrefabGarbageTruckData[prefabRef.m_Prefab];
			if (!m_EntityLookup.Exists(target.m_Target) || VehicleUtils.PathfindFailed(pathOwner))
			{
				if (VehicleUtils.IsStuck(pathOwner) || (garbageTruck.m_State & GarbageTruckFlags.Returning) != 0)
				{
					if (UnloadGarbage(jobIndex, vehicleEntity, prefabGarbageTruckData, owner.m_Owner, ref garbageTruck, instant: true))
					{
						m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					}
					return;
				}
				ReturnToDepot(owner, serviceDispatches, ref garbageTruck, ref car, ref pathOwner, ref target);
			}
			else if (VehicleUtils.PathEndReached(currentLane))
			{
				if ((garbageTruck.m_State & GarbageTruckFlags.Returning) != 0)
				{
					if (UnloadGarbage(jobIndex, vehicleEntity, prefabGarbageTruckData, owner.m_Owner, ref garbageTruck, instant: false))
					{
						m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					}
					return;
				}
				TryCollectGarbage(jobIndex, vehicleEntity, prefabGarbageTruckData, owner, ref garbageTruck, ref car, ref currentLane, target.m_Target);
				TryCollectGarbage(jobIndex, vehicleEntity, prefabGarbageTruckData, owner, ref garbageTruck, ref car, ref target);
				CheckServiceDispatches(vehicleEntity, serviceDispatches, owner, ref garbageTruck, ref pathOwner);
				if (!SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, owner, ref garbageTruck, ref car, ref currentLane, ref pathOwner, ref target))
				{
					ReturnToDepot(owner, serviceDispatches, ref garbageTruck, ref car, ref pathOwner, ref target);
				}
			}
			else
			{
				if (VehicleUtils.ParkingSpaceReached(currentLane, pathOwner))
				{
					if ((garbageTruck.m_State & GarbageTruckFlags.Returning) != 0)
					{
						if (UnloadGarbage(jobIndex, vehicleEntity, prefabGarbageTruckData, owner.m_Owner, ref garbageTruck, instant: false))
						{
							ParkCar(jobIndex, vehicleEntity, owner, ref garbageTruck, ref car, ref currentLane);
						}
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					}
					return;
				}
				if (VehicleUtils.WaypointReached(currentLane))
				{
					currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.Waypoint;
					TryCollectGarbage(jobIndex, vehicleEntity, prefabGarbageTruckData, owner, ref garbageTruck, ref car, ref currentLane, Entity.Null);
				}
				else if ((garbageTruck.m_State & GarbageTruckFlags.Unloading) != 0)
				{
					UnloadGarbage(jobIndex, vehicleEntity, prefabGarbageTruckData, owner.m_Owner, ref garbageTruck, instant: true);
				}
			}
			if ((garbageTruck.m_State & GarbageTruckFlags.Returning) == 0)
			{
				if (garbageTruck.m_Garbage >= prefabGarbageTruckData.m_GarbageCapacity || (garbageTruck.m_State & GarbageTruckFlags.Disabled) != 0)
				{
					ReturnToDepot(owner, serviceDispatches, ref garbageTruck, ref car, ref pathOwner, ref target);
				}
				else
				{
					CheckGarbagePresence(owner, ref currentLane, ref garbageTruck, ref car, navigationLanes);
				}
			}
			if (garbageTruck.m_Garbage + garbageTruck.m_EstimatedGarbage >= prefabGarbageTruckData.m_GarbageCapacity)
			{
				garbageTruck.m_State |= GarbageTruckFlags.EstimatedFull;
			}
			else
			{
				garbageTruck.m_State &= ~GarbageTruckFlags.EstimatedFull;
			}
			if (garbageTruck.m_Garbage < prefabGarbageTruckData.m_GarbageCapacity && (garbageTruck.m_State & GarbageTruckFlags.Disabled) == 0)
			{
				CheckServiceDispatches(vehicleEntity, serviceDispatches, owner, ref garbageTruck, ref pathOwner);
				if ((garbageTruck.m_State & GarbageTruckFlags.Returning) != 0)
				{
					SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, owner, ref garbageTruck, ref car, ref currentLane, ref pathOwner, ref target);
				}
				if (garbageTruck.m_RequestCount <= 1 && (garbageTruck.m_State & GarbageTruckFlags.EstimatedFull) == 0)
				{
					RequestTargetIfNeeded(jobIndex, vehicleEntity, ref garbageTruck);
				}
			}
			else
			{
				serviceDispatches.Clear();
			}
			if ((garbageTruck.m_State & GarbageTruckFlags.Unloading) == 0)
			{
				if (VehicleUtils.RequireNewPath(pathOwner))
				{
					FindNewPath(vehicleEntity, prefabRef, ref garbageTruck, ref currentLane, ref pathOwner, ref target);
				}
				else if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Stuck)) == 0)
				{
					CheckParkingSpace(vehicleEntity, ref random, ref currentLane, ref pathOwner, navigationLanes);
				}
			}
		}

		private void CheckParkingSpace(Entity entity, ref Unity.Mathematics.Random random, ref CarCurrentLane currentLane, ref PathOwner pathOwner, DynamicBuffer<CarNavigationLane> navigationLanes)
		{
			DynamicBuffer<PathElement> path = m_PathElements[entity];
			ComponentLookup<Blocker> blockerData = default(ComponentLookup<Blocker>);
			VehicleUtils.ValidateParkingSpace(entity, ref random, ref currentLane, ref pathOwner, navigationLanes, path, ref m_ParkedCarData, ref blockerData, ref m_CurveData, ref m_UnspawnedData, ref m_ParkingLaneData, ref m_GarageLaneData, ref m_ConnectionLaneData, ref m_PrefabRefData, ref m_PrefabParkingLaneData, ref m_PrefabObjectGeometryData, ref m_LaneObjects, ref m_LaneOverlaps, ignoreDriveways: false, ignoreDisabled: false, boardingOnly: false);
		}

		private void ParkCar(int jobIndex, Entity entity, Owner owner, ref Game.Vehicles.GarbageTruck garbageTruck, ref Car car, ref CarCurrentLane currentLane)
		{
			car.m_Flags &= ~(CarFlags.Warning | CarFlags.Working);
			garbageTruck.m_State &= GarbageTruckFlags.IndustrialWasteOnly;
			if (m_GarbageFacilityData.TryGetComponent(owner.m_Owner, out var componentData) && (componentData.m_Flags & (GarbageFacilityFlags.HasAvailableGarbageTrucks | GarbageFacilityFlags.HasAvailableSpace)) != (GarbageFacilityFlags.HasAvailableGarbageTrucks | GarbageFacilityFlags.HasAvailableSpace))
			{
				garbageTruck.m_State |= GarbageTruckFlags.Disabled;
			}
			m_CommandBuffer.RemoveComponent(jobIndex, entity, in m_MovingToParkedCarRemoveTypes);
			m_CommandBuffer.AddComponent(jobIndex, entity, in m_MovingToParkedAddTypes);
			m_CommandBuffer.SetComponent(jobIndex, entity, new ParkedCar(currentLane.m_Lane, currentLane.m_CurvePosition.x));
			if (m_ParkingLaneData.HasComponent(currentLane.m_Lane) && currentLane.m_ChangeLane == Entity.Null)
			{
				m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, currentLane.m_Lane);
			}
			else if (m_GarageLaneData.HasComponent(currentLane.m_Lane))
			{
				m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, currentLane.m_Lane);
				m_CommandBuffer.AddComponent(jobIndex, entity, new FixParkingLocation(currentLane.m_ChangeLane, entity));
			}
			else
			{
				m_CommandBuffer.AddComponent(jobIndex, entity, new FixParkingLocation(currentLane.m_ChangeLane, entity));
			}
		}

		private void FindNewPath(Entity vehicleEntity, PrefabRef prefabRef, ref Game.Vehicles.GarbageTruck garbageTruck, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			CarData carData = m_PrefabCarData[prefabRef.m_Prefab];
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = carData.m_MaxSpeed,
				m_WalkSpeed = 5.555556f,
				m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
				m_Methods = (PathMethod.Road | PathMethod.SpecialParking),
				m_ParkingTarget = VehicleUtils.GetParkingSource(vehicleEntity, currentLane, ref m_ParkingLaneData, ref m_ConnectionLaneData),
				m_ParkingDelta = currentLane.m_CurvePosition.z,
				m_ParkingSize = VehicleUtils.GetParkingSize(vehicleEntity, ref m_PrefabRefData, ref m_PrefabObjectGeometryData),
				m_IgnoredRules = (RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = (PathMethod.Road | PathMethod.SpecialParking),
				m_RoadTypes = RoadTypes.Car
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = PathMethod.Road,
				m_RoadTypes = RoadTypes.Car,
				m_Entity = target.m_Target
			};
			if ((garbageTruck.m_State & GarbageTruckFlags.Returning) != 0)
			{
				destination.m_Methods |= PathMethod.SpecialParking;
				destination.m_RandomCost = 30f;
			}
			VehicleUtils.SetupPathfind(item: new SetupQueueItem(vehicleEntity, parameters, origin, destination), currentLane: ref currentLane, pathOwner: ref pathOwner, queue: m_PathfindQueue);
		}

		private void CheckServiceDispatches(Entity vehicleEntity, DynamicBuffer<ServiceDispatch> serviceDispatches, Owner owner, ref Game.Vehicles.GarbageTruck garbageTruck, ref PathOwner pathOwner)
		{
			if (serviceDispatches.Length <= garbageTruck.m_RequestCount)
			{
				return;
			}
			int num = -1;
			Entity entity = Entity.Null;
			PathElement pathElement = default(PathElement);
			bool flag = false;
			int num2 = 0;
			if (garbageTruck.m_RequestCount >= 1 && (garbageTruck.m_State & GarbageTruckFlags.Returning) == 0)
			{
				DynamicBuffer<PathElement> dynamicBuffer = m_PathElements[vehicleEntity];
				num2 = 1;
				if (pathOwner.m_ElementIndex < dynamicBuffer.Length)
				{
					pathElement = dynamicBuffer[dynamicBuffer.Length - 1];
					flag = true;
				}
			}
			for (int i = num2; i < garbageTruck.m_RequestCount; i++)
			{
				Entity request = serviceDispatches[i].m_Request;
				if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
				{
					pathElement = bufferData[bufferData.Length - 1];
					flag = true;
				}
			}
			for (int j = garbageTruck.m_RequestCount; j < serviceDispatches.Length; j++)
			{
				Entity request2 = serviceDispatches[j].m_Request;
				if (!m_GarbageCollectionRequestData.HasComponent(request2))
				{
					continue;
				}
				GarbageCollectionRequest garbageCollectionRequest = m_GarbageCollectionRequestData[request2];
				if (flag && m_PathElements.TryGetBuffer(request2, out var bufferData2) && bufferData2.Length != 0)
				{
					PathElement pathElement2 = bufferData2[0];
					if (pathElement2.m_Target != pathElement.m_Target || pathElement2.m_TargetDelta.x != pathElement.m_TargetDelta.y)
					{
						continue;
					}
				}
				if (m_PrefabRefData.HasComponent(garbageCollectionRequest.m_Target) && garbageCollectionRequest.m_Priority > num)
				{
					num = garbageCollectionRequest.m_Priority;
					entity = request2;
				}
			}
			if (entity != Entity.Null)
			{
				serviceDispatches[garbageTruck.m_RequestCount++] = new ServiceDispatch(entity);
				PreAddCollectionRequests(entity, owner, ref garbageTruck);
			}
			if (serviceDispatches.Length > garbageTruck.m_RequestCount)
			{
				serviceDispatches.RemoveRange(garbageTruck.m_RequestCount, serviceDispatches.Length - garbageTruck.m_RequestCount);
			}
		}

		private void PreAddCollectionRequests(Entity request, Owner owner, ref Game.Vehicles.GarbageTruck garbageTruck)
		{
			if (!m_PathElements.TryGetBuffer(request, out var bufferData))
			{
				return;
			}
			m_ServiceDistricts.TryGetBuffer(owner.m_Owner, out var bufferData2);
			int dispatchIndex = BumpDispachIndex(request);
			Entity entity = Entity.Null;
			for (int i = 0; i < bufferData.Length; i++)
			{
				PathElement pathElement = bufferData[i];
				if (!m_EdgeLaneData.HasComponent(pathElement.m_Target))
				{
					entity = Entity.Null;
					continue;
				}
				Owner owner2 = m_OwnerData[pathElement.m_Target];
				if (!(owner2.m_Owner == entity))
				{
					entity = owner2.m_Owner;
					if (HasSidewalk(owner2.m_Owner))
					{
						garbageTruck.m_EstimatedGarbage += AddCollectionRequests(owner2.m_Owner, request, dispatchIndex, bufferData2, ref garbageTruck);
					}
				}
			}
		}

		private bool HasSidewalk(Entity owner)
		{
			if (m_SubLanes.HasBuffer(owner))
			{
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[owner];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					if (m_PedestrianLaneData.HasComponent(subLane))
					{
						return true;
					}
				}
			}
			return false;
		}

		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Vehicles.GarbageTruck garbageTruck)
		{
			if (!m_GarbageCollectionRequestData.HasComponent(garbageTruck.m_TargetRequest))
			{
				uint num = math.max(512u, 16u);
				if ((m_SimulationFrameIndex & (num - 1)) == 2)
				{
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_GarbageCollectionRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e, new GarbageCollectionRequest(entity, 1, ((garbageTruck.m_State & GarbageTruckFlags.IndustrialWasteOnly) != 0) ? GarbageCollectionRequestFlags.IndustrialWaste : ((GarbageCollectionRequestFlags)0)));
					m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
				}
			}
		}

		private bool SelectNextDispatch(int jobIndex, Entity vehicleEntity, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, Owner owner, ref Game.Vehicles.GarbageTruck garbageTruck, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if ((garbageTruck.m_State & GarbageTruckFlags.Returning) == 0 && garbageTruck.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				serviceDispatches.RemoveAt(0);
				garbageTruck.m_RequestCount--;
			}
			while (garbageTruck.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				Entity entity = Entity.Null;
				if (m_GarbageCollectionRequestData.TryGetComponent(request, out var componentData))
				{
					entity = componentData.m_Target;
				}
				if (!m_EntityLookup.Exists(entity))
				{
					serviceDispatches.RemoveAt(0);
					garbageTruck.m_EstimatedGarbage -= garbageTruck.m_EstimatedGarbage / garbageTruck.m_RequestCount;
					garbageTruck.m_RequestCount--;
					continue;
				}
				garbageTruck.m_State &= ~GarbageTruckFlags.Returning;
				car.m_Flags |= CarFlags.UsePublicTransportLanes;
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, vehicleEntity, completed: false, pathConsumed: true));
				if (m_GarbageCollectionRequestData.HasComponent(garbageTruck.m_TargetRequest))
				{
					e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(garbageTruck.m_TargetRequest, Entity.Null, completed: true));
				}
				if (m_PathElements.HasBuffer(request))
				{
					DynamicBuffer<PathElement> appendPath = m_PathElements[request];
					if (appendPath.Length != 0)
					{
						DynamicBuffer<PathElement> dynamicBuffer = m_PathElements[vehicleEntity];
						PathUtils.TrimPath(dynamicBuffer, ref pathOwner);
						float num = garbageTruck.m_PathElementTime * (float)dynamicBuffer.Length + m_PathInformationData[request].m_Duration;
						if (PathUtils.TryAppendPath(ref currentLane, navigationLanes, dynamicBuffer, appendPath, m_SlaveLaneData, m_OwnerData, m_SubLanes, out var appendedCount))
						{
							m_ServiceDistricts.TryGetBuffer(owner.m_Owner, out var bufferData);
							int dispatchIndex = BumpDispachIndex(request);
							int num2 = dynamicBuffer.Length - appendedCount;
							int num3 = 0;
							for (int i = 0; i < num2; i++)
							{
								PathElement pathElement = dynamicBuffer[i];
								if (m_PedestrianLaneData.HasComponent(pathElement.m_Target))
								{
									num3 += AddCollectionRequests(m_OwnerData[pathElement.m_Target].m_Owner, request, dispatchIndex, bufferData, ref garbageTruck);
								}
							}
							if (appendedCount > 0)
							{
								NativeArray<PathElement> nativeArray = new NativeArray<PathElement>(appendedCount, Allocator.Temp);
								for (int j = 0; j < appendedCount; j++)
								{
									nativeArray[j] = dynamicBuffer[num2 + j];
								}
								dynamicBuffer.RemoveRange(num2, appendedCount);
								Entity lastOwner = Entity.Null;
								for (int k = 0; k < nativeArray.Length; k++)
								{
									num3 += AddPathElement(dynamicBuffer, nativeArray[k], request, dispatchIndex, ref lastOwner, ref garbageTruck, bufferData);
								}
								nativeArray.Dispose();
							}
							if (garbageTruck.m_RequestCount == 1)
							{
								garbageTruck.m_EstimatedGarbage = num3;
							}
							car.m_Flags |= CarFlags.StayOnRoad;
							garbageTruck.m_PathElementTime = num / (float)math.max(1, dynamicBuffer.Length);
							target.m_Target = entity;
							VehicleUtils.ClearEndOfPath(ref currentLane, navigationLanes);
							return true;
						}
					}
				}
				VehicleUtils.SetTarget(ref pathOwner, ref target, entity);
				return true;
			}
			return false;
		}

		private int BumpDispachIndex(Entity request)
		{
			int result = 0;
			if (m_GarbageCollectionRequestData.TryGetComponent(request, out var componentData))
			{
				result = componentData.m_DispatchIndex + 1;
				m_ActionQueue.Enqueue(new GarbageAction
				{
					m_Type = GarbageActionType.BumpDispatchIndex,
					m_Request = request
				});
			}
			return result;
		}

		private void ReturnToDepot(Owner ownerData, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.GarbageTruck garbageTruck, ref Car car, ref PathOwner pathOwnerData, ref Target targetData)
		{
			serviceDispatches.Clear();
			garbageTruck.m_RequestCount = 0;
			garbageTruck.m_EstimatedGarbage = 0;
			garbageTruck.m_State |= GarbageTruckFlags.Returning;
			car.m_Flags &= ~(CarFlags.Warning | CarFlags.Working);
			VehicleUtils.SetTarget(ref pathOwnerData, ref targetData, ownerData.m_Owner);
		}

		private void ResetPath(int jobIndex, Entity vehicleEntity, PathInformation pathInformation, DynamicBuffer<ServiceDispatch> serviceDispatches, Owner owner, ref Unity.Mathematics.Random random, ref Game.Vehicles.GarbageTruck garbageTruck, ref Car carData, ref CarCurrentLane currentLane, ref PathOwner pathOwner)
		{
			DynamicBuffer<PathElement> path = m_PathElements[vehicleEntity];
			PathUtils.ResetPath(ref currentLane, path, m_SlaveLaneData, m_OwnerData, m_SubLanes);
			VehicleUtils.ResetParkingLaneStatus(vehicleEntity, ref currentLane, ref pathOwner, path, ref m_EntityLookup, ref m_CurveData, ref m_ParkingLaneData, ref m_CarLaneData, ref m_ConnectionLaneData, ref m_SpawnLocationData, ref m_PrefabRefData, ref m_PrefabSpawnLocationData);
			VehicleUtils.SetParkingCurvePos(vehicleEntity, ref random, currentLane, pathOwner, path, ref m_ParkedCarData, ref m_UnspawnedData, ref m_CurveData, ref m_ParkingLaneData, ref m_ConnectionLaneData, ref m_PrefabRefData, ref m_PrefabObjectGeometryData, ref m_PrefabParkingLaneData, ref m_LaneObjects, ref m_LaneOverlaps, ignoreDriveways: false);
			currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.Checked;
			if ((garbageTruck.m_State & GarbageTruckFlags.Returning) == 0 && garbageTruck.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				if (m_GarbageCollectionRequestData.HasComponent(request))
				{
					NativeArray<PathElement> nativeArray = new NativeArray<PathElement>(path.Length, Allocator.Temp);
					nativeArray.CopyFrom(path.AsNativeArray());
					path.Clear();
					m_ServiceDistricts.TryGetBuffer(owner.m_Owner, out var bufferData);
					Entity lastOwner = Entity.Null;
					int estimatedGarbage = 0;
					int dispatchIndex = BumpDispachIndex(request);
					for (int i = 0; i < nativeArray.Length; i++)
					{
						estimatedGarbage = AddPathElement(path, nativeArray[i], request, dispatchIndex, ref lastOwner, ref garbageTruck, bufferData);
					}
					if (garbageTruck.m_RequestCount == 1)
					{
						garbageTruck.m_EstimatedGarbage = estimatedGarbage;
					}
					nativeArray.Dispose();
				}
				carData.m_Flags |= CarFlags.StayOnRoad;
			}
			else
			{
				carData.m_Flags &= ~CarFlags.StayOnRoad;
			}
			carData.m_Flags |= CarFlags.UsePublicTransportLanes;
			garbageTruck.m_PathElementTime = pathInformation.m_Duration / (float)math.max(1, path.Length);
		}

		private int AddPathElement(DynamicBuffer<PathElement> path, PathElement pathElement, Entity request, int dispatchIndex, ref Entity lastOwner, ref Game.Vehicles.GarbageTruck garbageTruck, DynamicBuffer<ServiceDistrict> serviceDistricts)
		{
			int result = 0;
			if (!m_EdgeLaneData.HasComponent(pathElement.m_Target))
			{
				path.Add(pathElement);
				lastOwner = Entity.Null;
				return result;
			}
			Owner owner = m_OwnerData[pathElement.m_Target];
			if (owner.m_Owner == lastOwner)
			{
				path.Add(pathElement);
				return result;
			}
			lastOwner = owner.m_Owner;
			float curvePos = pathElement.m_TargetDelta.y;
			if (FindClosestSidewalk(pathElement.m_Target, owner.m_Owner, ref curvePos, out var sidewalk))
			{
				result = AddCollectionRequests(owner.m_Owner, request, dispatchIndex, serviceDistricts, ref garbageTruck);
				path.Add(pathElement);
				path.Add(new PathElement(sidewalk, curvePos));
			}
			else
			{
				path.Add(pathElement);
			}
			return result;
		}

		private bool FindClosestSidewalk(Entity lane, Entity owner, ref float curvePos, out Entity sidewalk)
		{
			bool result = false;
			sidewalk = Entity.Null;
			if (m_SubLanes.TryGetBuffer(owner, out var bufferData))
			{
				float3 position = MathUtils.Position(m_CurveData[lane].m_Bezier, curvePos);
				float num = float.MaxValue;
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity subLane = bufferData[i].m_SubLane;
					if (m_PedestrianLaneData.HasComponent(subLane))
					{
						float t;
						float num2 = MathUtils.Distance(MathUtils.Line(m_CurveData[subLane].m_Bezier), position, out t);
						if (num2 < num)
						{
							curvePos = t;
							sidewalk = subLane;
							num = num2;
							result = true;
						}
					}
				}
			}
			return result;
		}

		private int AddCollectionRequests(Entity edgeEntity, Entity request, int dispatchIndex, DynamicBuffer<ServiceDistrict> serviceDistricts, ref Game.Vehicles.GarbageTruck garbageTruck)
		{
			int num = 0;
			if (m_ConnectedBuildings.TryGetBuffer(edgeEntity, out var bufferData))
			{
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity building = bufferData[i].m_Building;
					if (m_GarbageProducerData.TryGetComponent(building, out var componentData) && ((garbageTruck.m_State & GarbageTruckFlags.IndustrialWasteOnly) == 0 || IsIndustrial(m_PrefabRefData[building].m_Prefab)) && AreaUtils.CheckServiceDistrict(building, serviceDistricts, ref m_CurrentDistrictData))
					{
						num += componentData.m_Garbage;
						m_ActionQueue.Enqueue(new GarbageAction
						{
							m_Type = GarbageActionType.AddRequest,
							m_Request = request,
							m_Target = building,
							m_Capacity = dispatchIndex
						});
					}
				}
			}
			return num;
		}

		private void CheckGarbagePresence(Owner owner, ref CarCurrentLane currentLane, ref Game.Vehicles.GarbageTruck garbageTruck, ref Car car, DynamicBuffer<CarNavigationLane> navigationLanes)
		{
			if ((garbageTruck.m_State & GarbageTruckFlags.ClearChecked) != 0)
			{
				if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Waypoint) != 0)
				{
					currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.Checked;
				}
				garbageTruck.m_State &= ~GarbageTruckFlags.ClearChecked;
			}
			if ((currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.Waypoint | Game.Vehicles.CarLaneFlags.Checked)) == Game.Vehicles.CarLaneFlags.Waypoint)
			{
				if (!CheckGarbagePresence(currentLane.m_Lane, owner, ref garbageTruck))
				{
					currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.Waypoint;
					car.m_Flags &= ~(CarFlags.Warning | CarFlags.Working);
					if (m_SlaveLaneData.HasComponent(currentLane.m_Lane))
					{
						currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.FixedLane;
					}
				}
				currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.Checked;
			}
			if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Waypoint) != 0)
			{
				car.m_Flags |= (CarFlags)((math.abs(currentLane.m_CurvePosition.x - currentLane.m_CurvePosition.z) < 0.5f) ? 520 : 8);
				return;
			}
			for (int i = 0; i < navigationLanes.Length; i++)
			{
				ref CarNavigationLane reference = ref navigationLanes.ElementAt(i);
				if ((reference.m_Flags & Game.Vehicles.CarLaneFlags.Waypoint) != 0 && (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Checked) == 0)
				{
					if (!CheckGarbagePresence(reference.m_Lane, owner, ref garbageTruck))
					{
						reference.m_Flags &= ~Game.Vehicles.CarLaneFlags.Waypoint;
						car.m_Flags &= ~CarFlags.Warning;
						if (m_SlaveLaneData.HasComponent(reference.m_Lane))
						{
							reference.m_Flags &= ~Game.Vehicles.CarLaneFlags.FixedLane;
						}
					}
					currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.Checked;
					car.m_Flags &= ~CarFlags.Working;
				}
				if ((reference.m_Flags & (Game.Vehicles.CarLaneFlags.Reserved | Game.Vehicles.CarLaneFlags.Waypoint)) != Game.Vehicles.CarLaneFlags.Reserved)
				{
					car.m_Flags &= ~CarFlags.Working;
					break;
				}
			}
		}

		private bool CheckGarbagePresence(Entity laneEntity, Owner owner, ref Game.Vehicles.GarbageTruck garbageTruck)
		{
			if (m_EdgeLaneData.HasComponent(laneEntity) && m_OwnerData.TryGetComponent(laneEntity, out var componentData) && m_ConnectedBuildings.TryGetBuffer(componentData.m_Owner, out var bufferData))
			{
				m_ServiceDistricts.TryGetBuffer(owner.m_Owner, out var bufferData2);
				for (int i = 0; i < bufferData.Length; i++)
				{
					Entity building = bufferData[i].m_Building;
					if (m_GarbageProducerData.TryGetComponent(building, out var componentData2) && componentData2.m_Garbage > m_GarbageParameters.m_CollectionGarbageLimit && ((garbageTruck.m_State & GarbageTruckFlags.IndustrialWasteOnly) == 0 || IsIndustrial(m_PrefabRefData[building].m_Prefab)) && AreaUtils.CheckServiceDistrict(building, bufferData2, ref m_CurrentDistrictData))
					{
						return true;
					}
				}
			}
			return false;
		}

		private void TryCollectGarbage(int jobIndex, Entity vehicleEntity, GarbageTruckData prefabGarbageTruckData, Owner owner, ref Game.Vehicles.GarbageTruck garbageTruck, ref Car car, ref CarCurrentLane currentLaneData, Entity ignoreBuilding)
		{
			if (garbageTruck.m_Garbage < prefabGarbageTruckData.m_GarbageCapacity)
			{
				TryCollectGarbageFromLane(jobIndex, vehicleEntity, prefabGarbageTruckData, owner, ref garbageTruck, ref car, currentLaneData.m_Lane, ignoreBuilding);
			}
		}

		private void TryCollectGarbage(int jobIndex, Entity vehicleEntity, GarbageTruckData prefabGarbageTruckData, Owner owner, ref Game.Vehicles.GarbageTruck garbageTruck, ref Car car, ref Target target)
		{
			if (garbageTruck.m_Garbage < prefabGarbageTruckData.m_GarbageCapacity)
			{
				m_ServiceDistricts.TryGetBuffer(owner.m_Owner, out var bufferData);
				TryCollectGarbageFromBuilding(jobIndex, vehicleEntity, prefabGarbageTruckData, ref garbageTruck, ref car, target.m_Target, bufferData);
			}
		}

		private void TryCollectGarbageFromLane(int jobIndex, Entity vehicleEntity, GarbageTruckData prefabGarbageTruckData, Owner owner, ref Game.Vehicles.GarbageTruck garbageTruck, ref Car car, Entity laneEntity, Entity ignoreBuilding)
		{
			if (!m_EdgeLaneData.HasComponent(laneEntity) || !m_OwnerData.TryGetComponent(laneEntity, out var componentData) || !m_ConnectedBuildings.TryGetBuffer(componentData.m_Owner, out var bufferData))
			{
				return;
			}
			bool flag = false;
			m_ServiceDistricts.TryGetBuffer(owner.m_Owner, out var bufferData2);
			for (int i = 0; i < bufferData.Length; i++)
			{
				Entity building = bufferData[i].m_Building;
				if (building != ignoreBuilding)
				{
					flag |= TryCollectGarbageFromBuilding(jobIndex, vehicleEntity, prefabGarbageTruckData, ref garbageTruck, ref car, building, bufferData2);
				}
			}
			if (flag)
			{
				m_ActionQueue.Enqueue(new GarbageAction
				{
					m_Type = GarbageActionType.ClearLane,
					m_Vehicle = vehicleEntity,
					m_Target = laneEntity
				});
			}
		}

		private bool TryCollectGarbageFromBuilding(int jobIndex, Entity vehicleEntity, GarbageTruckData prefabGarbageTruckData, ref Game.Vehicles.GarbageTruck garbageTruck, ref Car car, Entity buildingEntity, DynamicBuffer<ServiceDistrict> serviceDistricts)
		{
			if (m_GarbageProducerData.TryGetComponent(buildingEntity, out var componentData) && componentData.m_Garbage > m_GarbageParameters.m_CollectionGarbageLimit)
			{
				if ((garbageTruck.m_State & GarbageTruckFlags.IndustrialWasteOnly) != 0 && !IsIndustrial(m_PrefabRefData[buildingEntity].m_Prefab))
				{
					return false;
				}
				if (!AreaUtils.CheckServiceDistrict(buildingEntity, serviceDistricts, ref m_CurrentDistrictData))
				{
					return false;
				}
				m_ActionQueue.Enqueue(new GarbageAction
				{
					m_Type = GarbageActionType.Collect,
					m_Vehicle = vehicleEntity,
					m_Target = buildingEntity,
					m_Capacity = prefabGarbageTruckData.m_GarbageCapacity
				});
				if (componentData.m_Garbage >= m_GarbageParameters.m_RequestGarbageLimit)
				{
					QuantityUpdated(jobIndex, buildingEntity);
				}
				car.m_Flags |= CarFlags.Warning | CarFlags.Working;
				return true;
			}
			return false;
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

		private bool IsIndustrial(Entity prefab)
		{
			if (m_PrefabSpawnableBuildingData.TryGetComponent(prefab, out var componentData) && m_PrefabZoneData.TryGetComponent(componentData.m_ZonePrefab, out var componentData2))
			{
				return componentData2.m_AreaType == Game.Zones.AreaType.Industrial;
			}
			return false;
		}

		private bool UnloadGarbage(int jobIndex, Entity vehicleEntity, GarbageTruckData prefabGarbageTruckData, Entity facilityEntity, ref Game.Vehicles.GarbageTruck garbageTruck, bool instant)
		{
			if (garbageTruck.m_Garbage > 0 && m_GarbageFacilityData.HasComponent(facilityEntity))
			{
				m_ActionQueue.Enqueue(new GarbageAction
				{
					m_Type = GarbageActionType.Unload,
					m_Vehicle = vehicleEntity,
					m_Target = facilityEntity,
					m_MaxAmount = math.select(Mathf.RoundToInt((float)(prefabGarbageTruckData.m_UnloadRate * 16) / 60f), garbageTruck.m_Garbage, instant)
				});
				QuantityUpdated(jobIndex, facilityEntity);
				return false;
			}
			if ((garbageTruck.m_State & GarbageTruckFlags.Unloading) != 0)
			{
				garbageTruck.m_State &= ~GarbageTruckFlags.Unloading;
				m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(EffectsUpdated));
			}
			return true;
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct GarbageActionJob : IJob
	{
		public ComponentLookup<Game.Vehicles.GarbageTruck> m_GarbageTruckData;

		public ComponentLookup<GarbageCollectionRequest> m_GarbageCollectionRequestData;

		public ComponentLookup<GarbageProducer> m_GarbageProducerData;

		public BufferLookup<Game.Economy.Resources> m_EconomyResources;

		public BufferLookup<Efficiency> m_Efficiencies;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public GarbageParameterData m_GarbageParameters;

		public float m_GarbageEfficiencyPenalty;

		public NativeQueue<GarbageAction> m_ActionQueue;

		public IconCommandBuffer m_IconCommandBuffer;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			GarbageAction item;
			while (m_ActionQueue.TryDequeue(out item))
			{
				switch (item.m_Type)
				{
				case GarbageActionType.Collect:
				{
					Game.Vehicles.GarbageTruck value3 = m_GarbageTruckData[item.m_Vehicle];
					GarbageProducer value4 = m_GarbageProducerData[item.m_Target];
					int num = math.min(item.m_Capacity - value3.m_Garbage, value4.m_Garbage);
					if (num > 0)
					{
						value3.m_Garbage += num;
						value3.m_EstimatedGarbage = math.max(0, value3.m_EstimatedGarbage - num);
						value4.m_Garbage -= num;
						if ((value4.m_Flags & GarbageProducerFlags.GarbagePilingUpWarning) != GarbageProducerFlags.None && value4.m_Garbage <= m_GarbageParameters.m_WarningGarbageLimit)
						{
							m_IconCommandBuffer.Remove(item.m_Target, m_GarbageParameters.m_GarbageNotificationPrefab);
							value4.m_Flags &= ~GarbageProducerFlags.GarbagePilingUpWarning;
						}
						m_GarbageTruckData[item.m_Vehicle] = value3;
						m_GarbageProducerData[item.m_Target] = value4;
						if (m_Efficiencies.TryGetBuffer(item.m_Target, out var bufferData2))
						{
							float garbageEfficiencyFactor = GarbageAccumulationSystem.GetGarbageEfficiencyFactor(value4.m_Garbage, m_GarbageParameters, m_GarbageEfficiencyPenalty);
							BuildingUtils.SetEfficiencyFactor(bufferData2, EfficiencyFactor.Garbage, garbageEfficiencyFactor);
						}
					}
					break;
				}
				case GarbageActionType.Unload:
				{
					Game.Vehicles.GarbageTruck value2 = m_GarbageTruckData[item.m_Vehicle];
					int garbage = value2.m_Garbage;
					garbage = math.min(garbage, item.m_MaxAmount);
					if (garbage > 0)
					{
						value2.m_Garbage -= garbage;
						if (m_EconomyResources.HasBuffer(item.m_Target))
						{
							DynamicBuffer<Game.Economy.Resources> resources = m_EconomyResources[item.m_Target];
							EconomyUtils.AddResources(Resource.Garbage, garbage, resources);
						}
						if ((value2.m_State & GarbageTruckFlags.Unloading) == 0)
						{
							value2.m_State |= GarbageTruckFlags.Unloading;
							m_CommandBuffer.AddComponent(item.m_Vehicle, default(EffectsUpdated));
						}
						m_GarbageTruckData[item.m_Vehicle] = value2;
					}
					else if ((value2.m_State & GarbageTruckFlags.Unloading) != 0)
					{
						value2.m_State &= ~GarbageTruckFlags.Unloading;
						m_CommandBuffer.AddComponent(item.m_Vehicle, default(EffectsUpdated));
					}
					break;
				}
				case GarbageActionType.AddRequest:
				{
					GarbageProducer value5 = m_GarbageProducerData[item.m_Target];
					value5.m_CollectionRequest = item.m_Request;
					value5.m_DispatchIndex = (byte)item.m_Capacity;
					m_GarbageProducerData[item.m_Target] = value5;
					break;
				}
				case GarbageActionType.ClearLane:
				{
					if (!m_LaneObjects.TryGetBuffer(item.m_Target, out var bufferData))
					{
						break;
					}
					for (int i = 0; i < bufferData.Length; i++)
					{
						Entity laneObject = bufferData[i].m_LaneObject;
						if (laneObject != item.m_Vehicle && m_GarbageTruckData.TryGetComponent(laneObject, out var componentData))
						{
							componentData.m_State |= GarbageTruckFlags.ClearChecked;
							m_GarbageTruckData[laneObject] = componentData;
						}
					}
					break;
				}
				case GarbageActionType.BumpDispatchIndex:
				{
					GarbageCollectionRequest value = m_GarbageCollectionRequestData[item.m_Request];
					value.m_DispatchIndex++;
					m_GarbageCollectionRequestData[item.m_Request] = value;
					break;
				}
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Owner> __Game_Common_Owner_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> __Game_Objects_Unspawned_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Vehicles.GarbageTruck> __Game_Vehicles_GarbageTruck_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Car> __Game_Vehicles_Car_RW_ComponentTypeHandle;

		public ComponentTypeHandle<CarCurrentLane> __Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Target> __Game_Common_Target_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;

		public BufferTypeHandle<CarNavigationLane> __Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle;

		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Quantity> __Game_Objects_Quantity_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Unspawned> __Game_Objects_Unspawned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> __Game_Net_PedestrianLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarageLane> __Game_Net_GarageLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageTruckData> __Game_Prefabs_GarbageTruckData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageCollectionRequest> __Game_Simulation_GarbageCollectionRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.GarbageFacility> __Game_Buildings_GarbageFacility_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedBuilding> __Game_Buildings_ConnectedBuilding_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Objects.SubObject> __Game_Objects_SubObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<ServiceDistrict> __Game_Areas_ServiceDistrict_RO_BufferLookup;

		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RW_BufferLookup;

		public ComponentLookup<Game.Vehicles.GarbageTruck> __Game_Vehicles_GarbageTruck_RW_ComponentLookup;

		public ComponentLookup<GarbageCollectionRequest> __Game_Simulation_GarbageCollectionRequest_RW_ComponentLookup;

		public ComponentLookup<GarbageProducer> __Game_Buildings_GarbageProducer_RW_ComponentLookup;

		public BufferLookup<Game.Economy.Resources> __Game_Economy_Resources_RW_BufferLookup;

		public BufferLookup<Efficiency> __Game_Buildings_Efficiency_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Vehicles_GarbageTruck_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.GarbageTruck>();
			__Game_Vehicles_Car_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Car>();
			__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarCurrentLane>();
			__Game_Common_Target_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Target>();
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<CarNavigationLane>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Objects_Quantity_RO_ComponentLookup = state.GetComponentLookup<Quantity>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentLookup = state.GetComponentLookup<Unspawned>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_GarageLane_RO_ComponentLookup = state.GetComponentLookup<GarageLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_GarbageTruckData_RO_ComponentLookup = state.GetComponentLookup<GarbageTruckData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup = state.GetComponentLookup<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentLookup = state.GetComponentLookup<ZoneData>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Simulation_GarbageCollectionRequest_RO_ComponentLookup = state.GetComponentLookup<GarbageCollectionRequest>(isReadOnly: true);
			__Game_Buildings_GarbageProducer_RO_ComponentLookup = state.GetComponentLookup<GarbageProducer>(isReadOnly: true);
			__Game_Buildings_GarbageFacility_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.GarbageFacility>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
			__Game_Buildings_ConnectedBuilding_RO_BufferLookup = state.GetBufferLookup<ConnectedBuilding>(isReadOnly: true);
			__Game_Objects_SubObject_RO_BufferLookup = state.GetBufferLookup<Game.Objects.SubObject>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
			__Game_Areas_ServiceDistrict_RO_BufferLookup = state.GetBufferLookup<ServiceDistrict>(isReadOnly: true);
			__Game_Pathfind_PathElement_RW_BufferLookup = state.GetBufferLookup<PathElement>();
			__Game_Vehicles_GarbageTruck_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.GarbageTruck>();
			__Game_Simulation_GarbageCollectionRequest_RW_ComponentLookup = state.GetComponentLookup<GarbageCollectionRequest>();
			__Game_Buildings_GarbageProducer_RW_ComponentLookup = state.GetComponentLookup<GarbageProducer>();
			__Game_Economy_Resources_RW_BufferLookup = state.GetBufferLookup<Game.Economy.Resources>();
			__Game_Buildings_Efficiency_RW_BufferLookup = state.GetBufferLookup<Efficiency>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private IconCommandSystem m_IconCommandSystem;

	private ServiceFeeSystem m_ServiceFeeSystem;

	private SimulationSystem m_SimulationSystem;

	private CityStatisticsSystem m_CityStatisticsSystem;

	private EntityQuery m_VehicleQuery;

	private EntityArchetype m_GarbageCollectionRequestArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private ComponentTypeSet m_MovingToParkedCarRemoveTypes;

	private ComponentTypeSet m_MovingToParkedAddTypes;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_647374864_0;

	private EntityQuery __query_647374864_1;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 2;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_ServiceFeeSystem = base.World.GetOrCreateSystemManaged<ServiceFeeSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CityStatisticsSystem = base.World.GetOrCreateSystemManaged<CityStatisticsSystem>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadWrite<CarCurrentLane>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadWrite<Game.Vehicles.GarbageTruck>(), ComponentType.ReadWrite<Target>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>(), ComponentType.Exclude<OutOfControl>());
		m_GarbageCollectionRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<GarbageCollectionRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_HandleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(), ComponentType.ReadWrite<Game.Common.Event>());
		m_MovingToParkedCarRemoveTypes = new ComponentTypeSet(new ComponentType[13]
		{
			ComponentType.ReadWrite<Moving>(),
			ComponentType.ReadWrite<TransformFrame>(),
			ComponentType.ReadWrite<InterpolatedTransform>(),
			ComponentType.ReadWrite<Swaying>(),
			ComponentType.ReadWrite<CarNavigation>(),
			ComponentType.ReadWrite<CarNavigationLane>(),
			ComponentType.ReadWrite<CarCurrentLane>(),
			ComponentType.ReadWrite<PathOwner>(),
			ComponentType.ReadWrite<Target>(),
			ComponentType.ReadWrite<Blocker>(),
			ComponentType.ReadWrite<PathElement>(),
			ComponentType.ReadWrite<PathInformation>(),
			ComponentType.ReadWrite<ServiceDispatch>()
		});
		m_MovingToParkedAddTypes = new ComponentTypeSet(ComponentType.ReadWrite<ParkedCar>(), ComponentType.ReadWrite<Stopped>(), ComponentType.ReadWrite<Updated>());
		RequireForUpdate(m_VehicleQuery);
		RequireForUpdate<GarbageParameterData>();
		RequireForUpdate<ServiceFeeParameterData>();
		RequireForUpdate<BuildingEfficiencyParameterData>();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		GarbageParameterData singleton = __query_647374864_0.GetSingleton<GarbageParameterData>();
		NativeQueue<GarbageAction> actionQueue = new NativeQueue<GarbageAction>(Allocator.TempJob);
		GarbageTruckTickJob jobData = new GarbageTruckTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GarbageTruckType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_GarbageTruck_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Car_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_QuantityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Quantity_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnspawnedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarageLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_GarageLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGarbageTruckData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GarbageTruckData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnableBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabZoneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageCollectionRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_GarbageCollectionRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_GarbageFacility_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedBuildings = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_ConnectedBuilding_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Objects_SubObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneOverlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup, ref base.CheckedStateRef),
			m_ServiceDistricts = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Areas_ServiceDistrict_RO_BufferLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_SimulationFrameIndex = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_GarbageCollectionRequestArchetype = m_GarbageCollectionRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_MovingToParkedCarRemoveTypes = m_MovingToParkedCarRemoveTypes,
			m_MovingToParkedAddTypes = m_MovingToParkedAddTypes,
			m_GarbageParameters = singleton,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
			m_ActionQueue = actionQueue.AsParallelWriter()
		};
		GarbageActionJob jobData2 = new GarbageActionJob
		{
			m_GarbageTruckData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_GarbageTruck_RW_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageCollectionRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_GarbageCollectionRequest_RW_ComponentLookup, ref base.CheckedStateRef),
			m_GarbageProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_GarbageProducer_RW_ComponentLookup, ref base.CheckedStateRef),
			m_EconomyResources = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Economy_Resources_RW_BufferLookup, ref base.CheckedStateRef),
			m_Efficiencies = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_Efficiency_RW_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_GarbageParameters = singleton,
			m_GarbageEfficiencyPenalty = __query_647374864_1.GetSingleton<BuildingEfficiencyParameterData>().m_GarbagePenalty,
			m_ActionQueue = actionQueue,
			m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer(),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_VehicleQuery, base.Dependency);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		actionQueue.Dispose(jobHandle2);
		m_PathfindSetupSystem.AddQueueWriter(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
		m_IconCommandSystem.AddCommandBufferWriter(jobHandle2);
		m_ServiceFeeSystem.AddQueueWriter(jobHandle2);
		m_CityStatisticsSystem.AddWriter(jobHandle2);
		base.Dependency = jobHandle2;
	}

	[MethodImpl(MethodImplOptions.AggressiveInlining)]
	private void __AssignQueries(ref SystemState state)
	{
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<GarbageParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_647374864_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<BuildingEfficiencyParameterData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_647374864_1 = entityQueryBuilder2.Build(ref state);
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
	public GarbageTruckAISystem()
	{
	}
}
