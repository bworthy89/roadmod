using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
using Game.Buildings;
using Game.Common;
using Game.Events;
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
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class FireEngineAISystem : GameSystemBase
{
	private struct FireExtinguishing
	{
		public Entity m_Vehicle;

		public Entity m_Target;

		public Entity m_Request;

		public float m_FireIntensityDelta;

		public float m_WaterDamageDelta;

		public float m_DestroyedClearDelta;

		public FireExtinguishing(Entity vehicle, Entity target, Entity request, float intensityDelta, float waterDamageDelta, float destroyedClearDelta)
		{
			m_Vehicle = vehicle;
			m_Target = target;
			m_Request = request;
			m_FireIntensityDelta = intensityDelta;
			m_WaterDamageDelta = waterDamageDelta;
			m_DestroyedClearDelta = destroyedClearDelta;
		}
	}

	[BurstCompile]
	private struct FireEngineTickJob : IJobChunk
	{
		private struct ObjectRequestIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public float3 m_Position;

			public float m_Spread;

			public Entity m_Vehicle;

			public Entity m_Request;

			public ComponentLookup<Transform> m_TransformData;

			public ComponentLookup<OnFire> m_OnFireData;

			public ComponentLookup<RescueTarget> m_RescueTargetData;

			public ComponentLookup<FireRescueRequest> m_FireRescueRequestData;

			public NativeQueue<FireExtinguishing>.ParallelWriter m_ExtinguishingQueue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity objectEntity)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds, m_Bounds) || (!m_OnFireData.HasComponent(objectEntity) && !m_RescueTargetData.HasComponent(objectEntity)) || math.distance(m_TransformData[objectEntity].m_Position, m_Position) > m_Spread)
				{
					return;
				}
				if (m_OnFireData.HasComponent(objectEntity))
				{
					if (m_OnFireData[objectEntity].m_RescueRequest != m_Request)
					{
						m_ExtinguishingQueue.Enqueue(new FireExtinguishing(m_Vehicle, objectEntity, m_Request, 0f, 0f, 0f));
					}
				}
				else if (m_RescueTargetData.HasComponent(objectEntity) && m_RescueTargetData[objectEntity].m_Request != m_Request)
				{
					m_ExtinguishingQueue.Enqueue(new FireExtinguishing(m_Vehicle, objectEntity, m_Request, 0f, 0f, 0f));
				}
			}
		}

		private struct ObjectExtinguishIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public float3 m_Position;

			public float m_Spread;

			public float m_ExtinguishRate;

			public float m_ClearRate;

			public Entity m_Vehicle;

			public Entity m_Target;

			public ComponentLookup<Transform> m_TransformData;

			public ComponentLookup<OnFire> m_OnFireData;

			public ComponentLookup<Destroyed> m_DestroyedData;

			public ComponentLookup<RescueTarget> m_RescueTargetData;

			public ComponentLookup<FireRescueRequest> m_FireRescueRequestData;

			public ComponentLookup<Building> m_BuildingData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public EventHelpers.StructuralIntegrityData m_StructuralIntegrityData;

			public NativeQueue<FireExtinguishing>.ParallelWriter m_ExtinguishingQueue;

			public Entity m_ExtinguishResult;

			public Entity m_ClearResult;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity objectEntity)
			{
				if (MathUtils.Intersect(bounds.m_Bounds, m_Bounds) && (m_OnFireData.HasComponent(objectEntity) || m_RescueTargetData.HasComponent(objectEntity)) && !(objectEntity == m_Target) && !(math.distance(m_TransformData[objectEntity].m_Position, m_Position) > m_Spread))
				{
					TryExtinguish(objectEntity);
				}
			}

			public void TryExtinguish(Entity entity)
			{
				if (m_OnFireData.HasComponent(entity))
				{
					PrefabRef prefabRef = m_PrefabRefData[entity];
					if (m_OnFireData[entity].m_Intensity > 0f)
					{
						float structuralIntegrity = m_StructuralIntegrityData.GetStructuralIntegrity(prefabRef.m_Prefab, m_BuildingData.HasComponent(entity));
						float num = 4f / 15f * m_ExtinguishRate;
						float waterDamageDelta = num * 10f / structuralIntegrity;
						m_ExtinguishingQueue.Enqueue(new FireExtinguishing(m_Vehicle, entity, Entity.Null, 0f - num, waterDamageDelta, 0f));
						if (m_ExtinguishResult == Entity.Null)
						{
							m_ExtinguishResult = entity;
						}
					}
				}
				else
				{
					if (!m_DestroyedData.HasComponent(entity))
					{
						return;
					}
					Destroyed destroyed = m_DestroyedData[entity];
					if (destroyed.m_Cleared >= 0f && destroyed.m_Cleared < 1f)
					{
						float destroyedClearDelta = 4f / 15f * m_ClearRate;
						m_ExtinguishingQueue.Enqueue(new FireExtinguishing(m_Vehicle, entity, Entity.Null, 0f, 0f, destroyedClearDelta));
						if (m_ClearResult == Entity.Null)
						{
							m_ClearResult = entity;
						}
					}
				}
			}
		}

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		[ReadOnly]
		public ComponentTypeHandle<Stopped> m_StoppedType;

		public ComponentTypeHandle<Game.Vehicles.FireEngine> m_FireEngineType;

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
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<GarageLane> m_GarageLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public ComponentLookup<FireEngineData> m_PrefabFireEngineData;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> m_FireRescueRequestData;

		[ReadOnly]
		public ComponentLookup<OnFire> m_OnFireData;

		[ReadOnly]
		public ComponentLookup<Owner> m_OwnerData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<RescueTarget> m_RescueTargetData;

		[ReadOnly]
		public ComponentLookup<Building> m_BuildingData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.FireStation> m_FireStationData;

		[ReadOnly]
		public ComponentLookup<Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Unspawned> m_UnspawnedData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public BufferLookup<LaneOverlap> m_LaneOverlaps;

		[NativeDisableParallelForRestriction]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public EntityArchetype m_FireRescueRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedCarRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedAddTypes;

		[ReadOnly]
		public uint m_SimulationFrameIndex;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EventHelpers.StructuralIntegrityData m_StructuralIntegrityData;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public NativeQueue<FireExtinguishing>.ParallelWriter m_ExtinguishingQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<PathInformation> nativeArray5 = chunk.GetNativeArray(ref m_PathInformationType);
			NativeArray<CarCurrentLane> nativeArray6 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<Game.Vehicles.FireEngine> nativeArray7 = chunk.GetNativeArray(ref m_FireEngineType);
			NativeArray<Car> nativeArray8 = chunk.GetNativeArray(ref m_CarType);
			NativeArray<Target> nativeArray9 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<PathOwner> nativeArray10 = chunk.GetNativeArray(ref m_PathOwnerType);
			BufferAccessor<CarNavigationLane> bufferAccessor = chunk.GetBufferAccessor(ref m_CarNavigationLaneType);
			BufferAccessor<ServiceDispatch> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			bool isStopped = chunk.Has(ref m_StoppedType);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Owner owner = nativeArray2[i];
				Transform transform = nativeArray3[i];
				PrefabRef prefabRef = nativeArray4[i];
				PathInformation pathInformation = nativeArray5[i];
				Game.Vehicles.FireEngine fireEngine = nativeArray7[i];
				Car car = nativeArray8[i];
				CarCurrentLane currentLane = nativeArray6[i];
				PathOwner pathOwner = nativeArray10[i];
				Target target = nativeArray9[i];
				DynamicBuffer<CarNavigationLane> navigationLanes = bufferAccessor[i];
				DynamicBuffer<ServiceDispatch> serviceDispatches = bufferAccessor2[i];
				VehicleUtils.CheckUnspawned(unfilteredChunkIndex, entity, currentLane, isUnspawned, m_CommandBuffer);
				Tick(unfilteredChunkIndex, entity, owner, transform, prefabRef, pathInformation, navigationLanes, serviceDispatches, isStopped, ref random, ref fireEngine, ref car, ref currentLane, ref pathOwner, ref target);
				nativeArray7[i] = fireEngine;
				nativeArray8[i] = car;
				nativeArray6[i] = currentLane;
				nativeArray10[i] = pathOwner;
				nativeArray9[i] = target;
			}
		}

		private void Tick(int jobIndex, Entity vehicleEntity, Owner owner, Transform transform, PrefabRef prefabRef, PathInformation pathInformation, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, bool isStopped, ref Random random, ref Game.Vehicles.FireEngine fireEngine, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if (VehicleUtils.ResetUpdatedPath(ref pathOwner))
			{
				ResetPath(jobIndex, vehicleEntity, ref random, pathInformation, serviceDispatches, ref fireEngine, ref car, ref currentLane, ref pathOwner);
			}
			FireEngineData fireEngineData = m_PrefabFireEngineData[prefabRef.m_Prefab];
			if (!m_EntityLookup.Exists(target.m_Target) || VehicleUtils.PathfindFailed(pathOwner))
			{
				if (VehicleUtils.IsStuck(pathOwner) || (fireEngine.m_State & FireEngineFlags.Returning) != 0)
				{
					m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					return;
				}
				ReturnToDepot(jobIndex, vehicleEntity, owner, serviceDispatches, ref fireEngine, ref car, ref pathOwner, ref target);
			}
			else if (VehicleUtils.PathEndReached(currentLane))
			{
				if ((fireEngine.m_State & FireEngineFlags.Returning) != 0)
				{
					m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					return;
				}
				if ((fireEngine.m_State & (FireEngineFlags.Extinguishing | FireEngineFlags.Rescueing)) == 0 && !BeginExtinguishing(jobIndex, vehicleEntity, isStopped, ref fireEngine, ref currentLane, ref target))
				{
					CheckServiceDispatches(vehicleEntity, serviceDispatches, fireEngineData, ref fireEngine, ref pathOwner);
					if ((fireEngine.m_State & (FireEngineFlags.Empty | FireEngineFlags.Disabled)) != 0 || !SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, ref fireEngine, ref car, ref currentLane, ref pathOwner, ref target))
					{
						ReturnToDepot(jobIndex, vehicleEntity, owner, serviceDispatches, ref fireEngine, ref car, ref pathOwner, ref target);
					}
				}
			}
			else
			{
				if (VehicleUtils.ParkingSpaceReached(currentLane, pathOwner))
				{
					if ((fireEngine.m_State & FireEngineFlags.Returning) != 0)
					{
						ParkCar(jobIndex, vehicleEntity, owner, fireEngineData, ref fireEngine, ref car, ref currentLane);
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					}
					return;
				}
				if (isStopped)
				{
					StartVehicle(jobIndex, vehicleEntity, ref currentLane);
				}
				else if ((car.m_Flags & CarFlags.Emergency) != 0 && (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.IsBlocked) != 0 && IsCloseEnough(transform, ref target))
				{
					EndNavigation(vehicleEntity, ref currentLane, ref pathOwner, navigationLanes);
				}
			}
			if (fireEngineData.m_ExtinguishingCapacity != 0f && fireEngine.m_RequestCount <= 1)
			{
				if (fireEngine.m_RequestCount == 1 && m_OnFireData.TryGetComponent(target.m_Target, out var componentData) && componentData.m_Intensity > 0f)
				{
					fireEngine.m_State |= FireEngineFlags.EstimatedEmpty;
				}
				else
				{
					fireEngine.m_State &= ~FireEngineFlags.EstimatedEmpty;
				}
			}
			if ((fireEngine.m_State & FireEngineFlags.Empty) != 0)
			{
				serviceDispatches.Clear();
			}
			else
			{
				CheckServiceDispatches(vehicleEntity, serviceDispatches, fireEngineData, ref fireEngine, ref pathOwner);
				if (fireEngine.m_RequestCount <= 1 && (fireEngine.m_State & FireEngineFlags.EstimatedEmpty) == 0)
				{
					RequestTargetIfNeeded(jobIndex, vehicleEntity, ref fireEngine);
				}
			}
			if ((fireEngine.m_State & (FireEngineFlags.Extinguishing | FireEngineFlags.Rescueing)) != 0)
			{
				if (!TryExtinguishFire(vehicleEntity, fireEngineData, ref fireEngine, ref target) && ((fireEngine.m_State & (FireEngineFlags.Empty | FireEngineFlags.Disabled)) != 0 || !SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, ref fireEngine, ref car, ref currentLane, ref pathOwner, ref target)))
				{
					ReturnToDepot(jobIndex, vehicleEntity, owner, serviceDispatches, ref fireEngine, ref car, ref pathOwner, ref target);
				}
			}
			else if ((fireEngine.m_State & (FireEngineFlags.Returning | FireEngineFlags.Empty | FireEngineFlags.Disabled)) == FireEngineFlags.Returning)
			{
				SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, ref fireEngine, ref car, ref currentLane, ref pathOwner, ref target);
			}
			if ((car.m_Flags & CarFlags.Emergency) != 0)
			{
				TryAddRequests(vehicleEntity, fireEngineData, serviceDispatches, ref fireEngine, ref target);
			}
			if ((fireEngine.m_State & (FireEngineFlags.Extinguishing | FireEngineFlags.Rescueing)) != 0)
			{
				return;
			}
			if (VehicleUtils.RequireNewPath(pathOwner))
			{
				if (isStopped && (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.ParkingSpace) == 0)
				{
					currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.EndOfPath;
				}
				else
				{
					FindNewPath(vehicleEntity, prefabRef, ref fireEngine, ref currentLane, ref pathOwner, ref target);
				}
			}
			else if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Stuck)) == 0)
			{
				CheckParkingSpace(vehicleEntity, ref random, ref currentLane, ref pathOwner, navigationLanes);
			}
		}

		private void CheckParkingSpace(Entity entity, ref Random random, ref CarCurrentLane currentLane, ref PathOwner pathOwner, DynamicBuffer<CarNavigationLane> navigationLanes)
		{
			DynamicBuffer<PathElement> path = m_PathElements[entity];
			ComponentLookup<Blocker> blockerData = default(ComponentLookup<Blocker>);
			VehicleUtils.ValidateParkingSpace(entity, ref random, ref currentLane, ref pathOwner, navigationLanes, path, ref m_ParkedCarData, ref blockerData, ref m_CurveData, ref m_UnspawnedData, ref m_ParkingLaneData, ref m_GarageLaneData, ref m_ConnectionLaneData, ref m_PrefabRefData, ref m_PrefabParkingLaneData, ref m_PrefabObjectGeometryData, ref m_LaneObjects, ref m_LaneOverlaps, ignoreDriveways: false, ignoreDisabled: false, boardingOnly: false);
		}

		private void ParkCar(int jobIndex, Entity entity, Owner owner, FireEngineData fireEngineData, ref Game.Vehicles.FireEngine fireEngine, ref Car car, ref CarCurrentLane currentLane)
		{
			car.m_Flags &= ~CarFlags.Emergency;
			fireEngine.m_State = (FireEngineFlags)0u;
			fireEngine.m_ExtinguishingAmount = fireEngineData.m_ExtinguishingCapacity;
			if (m_FireStationData.TryGetComponent(owner.m_Owner, out var componentData))
			{
				if ((componentData.m_Flags & FireStationFlags.HasFreeFireEngines) == 0)
				{
					fireEngine.m_State |= FireEngineFlags.Disabled;
				}
				if ((componentData.m_Flags & FireStationFlags.DisasterResponseAvailable) != 0)
				{
					fireEngine.m_State |= FireEngineFlags.DisasterResponse;
				}
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

		private void EndNavigation(Entity vehicleEntity, ref CarCurrentLane currentLane, ref PathOwner pathOwner, DynamicBuffer<CarNavigationLane> navigationLanes)
		{
			currentLane.m_CurvePosition.z = currentLane.m_CurvePosition.y;
			currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.EndOfPath;
			navigationLanes.Clear();
			pathOwner.m_ElementIndex = 0;
			m_PathElements[vehicleEntity].Clear();
		}

		private void StopVehicle(int jobIndex, Entity entity, ref CarCurrentLane currentLaneData)
		{
			m_CommandBuffer.RemoveComponent<Moving>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<TransformFrame>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<InterpolatedTransform>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<Swaying>(jobIndex, entity);
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Stopped));
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Updated));
			if (m_CarLaneData.HasComponent(currentLaneData.m_Lane))
			{
				m_CommandBuffer.AddComponent(jobIndex, currentLaneData.m_Lane, default(PathfindUpdated));
			}
			if (m_CarLaneData.HasComponent(currentLaneData.m_ChangeLane))
			{
				m_CommandBuffer.AddComponent(jobIndex, currentLaneData.m_ChangeLane, default(PathfindUpdated));
			}
		}

		private void StartVehicle(int jobIndex, Entity entity, ref CarCurrentLane currentLaneData)
		{
			m_CommandBuffer.RemoveComponent<Stopped>(jobIndex, entity);
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Moving));
			m_CommandBuffer.AddBuffer<TransformFrame>(jobIndex, entity);
			m_CommandBuffer.AddComponent(jobIndex, entity, default(InterpolatedTransform));
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Swaying));
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Updated));
			if (m_CarLaneData.HasComponent(currentLaneData.m_Lane))
			{
				m_CommandBuffer.AddComponent(jobIndex, currentLaneData.m_Lane, default(PathfindUpdated));
			}
			if (m_CarLaneData.HasComponent(currentLaneData.m_ChangeLane))
			{
				m_CommandBuffer.AddComponent(jobIndex, currentLaneData.m_ChangeLane, default(PathfindUpdated));
			}
		}

		private void FindNewPath(Entity vehicleEntity, PrefabRef prefabRef, ref Game.Vehicles.FireEngine fireEngine, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			CarData carData = m_PrefabCarData[prefabRef.m_Prefab];
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = carData.m_MaxSpeed,
				m_WalkSpeed = 5.555556f,
				m_Methods = (PathMethod.Road | PathMethod.Offroad | PathMethod.SpecialParking),
				m_ParkingTarget = VehicleUtils.GetParkingSource(vehicleEntity, currentLane, ref m_ParkingLaneData, ref m_ConnectionLaneData),
				m_ParkingDelta = currentLane.m_CurvePosition.z,
				m_ParkingSize = VehicleUtils.GetParkingSize(vehicleEntity, ref m_PrefabRefData, ref m_PrefabObjectGeometryData),
				m_IgnoredRules = (RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidPrivateTraffic | VehicleUtils.GetIgnoredPathfindRules(carData))
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = (PathMethod.Road | PathMethod.Offroad | PathMethod.SpecialParking),
				m_RoadTypes = RoadTypes.Car
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = (PathMethod.Road | PathMethod.Offroad),
				m_RoadTypes = RoadTypes.Car,
				m_Entity = target.m_Target
			};
			if ((fireEngine.m_State & FireEngineFlags.Returning) == 0)
			{
				parameters.m_Weights = new PathfindWeights(1f, 0f, 0f, 0f);
				parameters.m_IgnoredRules |= RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidHeavyTraffic;
				destination.m_Value2 = 30f;
			}
			else
			{
				parameters.m_Weights = new PathfindWeights(1f, 1f, 1f, 1f);
				destination.m_Methods |= PathMethod.SpecialParking;
				destination.m_RandomCost = 30f;
			}
			VehicleUtils.SetupPathfind(item: new SetupQueueItem(vehicleEntity, parameters, origin, destination), currentLane: ref currentLane, pathOwner: ref pathOwner, queue: m_PathfindQueue);
		}

		private void CheckServiceDispatches(Entity vehicleEntity, DynamicBuffer<ServiceDispatch> serviceDispatches, FireEngineData prefabFireEngineData, ref Game.Vehicles.FireEngine fireEngine, ref PathOwner pathOwner)
		{
			if (serviceDispatches.Length <= fireEngine.m_RequestCount)
			{
				return;
			}
			float num = -1f;
			Entity entity = Entity.Null;
			PathElement pathElement = default(PathElement);
			bool flag = false;
			int num2 = 0;
			if (fireEngine.m_RequestCount >= 1 && (fireEngine.m_State & FireEngineFlags.Returning) == 0)
			{
				DynamicBuffer<PathElement> dynamicBuffer = m_PathElements[vehicleEntity];
				num2 = 1;
				if (pathOwner.m_ElementIndex < dynamicBuffer.Length)
				{
					pathElement = dynamicBuffer[dynamicBuffer.Length - 1];
					flag = true;
				}
			}
			for (int i = num2; i < fireEngine.m_RequestCount; i++)
			{
				Entity request = serviceDispatches[i].m_Request;
				if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
				{
					pathElement = bufferData[bufferData.Length - 1];
					flag = true;
				}
			}
			for (int j = fireEngine.m_RequestCount; j < serviceDispatches.Length; j++)
			{
				Entity request2 = serviceDispatches[j].m_Request;
				if (!m_FireRescueRequestData.HasComponent(request2))
				{
					continue;
				}
				FireRescueRequest fireRescueRequest = m_FireRescueRequestData[request2];
				if (flag && m_PathElements.TryGetBuffer(request2, out var bufferData2) && bufferData2.Length != 0)
				{
					PathElement pathElement2 = bufferData2[0];
					if (pathElement2.m_Target != pathElement.m_Target || pathElement2.m_TargetDelta.x != pathElement.m_TargetDelta.y)
					{
						continue;
					}
				}
				if (m_PrefabRefData.HasComponent(fireRescueRequest.m_Target) && fireRescueRequest.m_Priority > num)
				{
					num = fireRescueRequest.m_Priority;
					entity = request2;
				}
			}
			if (entity != Entity.Null)
			{
				if (prefabFireEngineData.m_ExtinguishingCapacity != 0f)
				{
					FireRescueRequest fireRescueRequest2 = m_FireRescueRequestData[entity];
					if (m_OnFireData.TryGetComponent(fireRescueRequest2.m_Target, out var componentData) && componentData.m_Intensity > 0f)
					{
						fireEngine.m_State |= FireEngineFlags.EstimatedEmpty;
					}
					else if (fireEngine.m_RequestCount == 0)
					{
						fireEngine.m_State &= ~FireEngineFlags.EstimatedEmpty;
					}
				}
				serviceDispatches[fireEngine.m_RequestCount++] = new ServiceDispatch(entity);
			}
			if (serviceDispatches.Length > fireEngine.m_RequestCount)
			{
				serviceDispatches.RemoveRange(fireEngine.m_RequestCount, serviceDispatches.Length - fireEngine.m_RequestCount);
			}
		}

		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Vehicles.FireEngine fireEngine)
		{
			if (!m_FireRescueRequestData.HasComponent(fireEngine.m_TargetRequest))
			{
				uint num = math.max(64u, 16u);
				if ((m_SimulationFrameIndex & (num - 1)) == 4)
				{
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_FireRescueRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e, new FireRescueRequest(entity, 1f, FireRescueRequestType.Fire));
					m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(4u));
				}
			}
		}

		private bool SelectNextDispatch(int jobIndex, Entity vehicleEntity, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.FireEngine fireEngine, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if ((fireEngine.m_State & FireEngineFlags.Returning) == 0 && fireEngine.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				serviceDispatches.RemoveAt(0);
				fireEngine.m_RequestCount--;
			}
			while (fireEngine.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				Entity entity = Entity.Null;
				if (m_FireRescueRequestData.TryGetComponent(request, out var componentData))
				{
					entity = componentData.m_Target;
				}
				if (componentData.m_Type == FireRescueRequestType.Fire)
				{
					if (!m_OnFireData.TryGetComponent(entity, out var componentData2) || componentData2.m_Intensity == 0f)
					{
						entity = Entity.Null;
					}
				}
				else if (!m_RescueTargetData.HasComponent(entity))
				{
					entity = Entity.Null;
				}
				if (entity == Entity.Null)
				{
					serviceDispatches.RemoveAt(0);
					fireEngine.m_RequestCount--;
					continue;
				}
				fireEngine.m_State &= ~(FireEngineFlags.Extinguishing | FireEngineFlags.Rescueing);
				fireEngine.m_State &= ~FireEngineFlags.Returning;
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, vehicleEntity, completed: false, pathConsumed: true));
				if (m_FireRescueRequestData.HasComponent(fireEngine.m_TargetRequest))
				{
					e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(fireEngine.m_TargetRequest, Entity.Null, completed: true));
				}
				if (m_PathElements.HasBuffer(request))
				{
					DynamicBuffer<PathElement> appendPath = m_PathElements[request];
					if (appendPath.Length != 0)
					{
						DynamicBuffer<PathElement> dynamicBuffer = m_PathElements[vehicleEntity];
						PathUtils.TrimPath(dynamicBuffer, ref pathOwner);
						float num = fireEngine.m_PathElementTime * (float)dynamicBuffer.Length + m_PathInformationData[request].m_Duration;
						if (PathUtils.TryAppendPath(ref currentLane, navigationLanes, dynamicBuffer, appendPath, m_SlaveLaneData, m_OwnerData, m_SubLanes))
						{
							fireEngine.m_PathElementTime = num / (float)math.max(1, dynamicBuffer.Length);
							target.m_Target = entity;
							VehicleUtils.ClearEndOfPath(ref currentLane, navigationLanes);
							car.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad | CarFlags.UsePublicTransportLanes;
							m_CommandBuffer.AddComponent<EffectsUpdated>(jobIndex, vehicleEntity);
							return true;
						}
					}
				}
				VehicleUtils.SetTarget(ref pathOwner, ref target, entity);
				m_CommandBuffer.AddComponent<EffectsUpdated>(jobIndex, vehicleEntity);
				return true;
			}
			return false;
		}

		private bool BeginExtinguishing(int jobIndex, Entity entity, bool isStopped, ref Game.Vehicles.FireEngine fireEngine, ref CarCurrentLane currentLaneData, ref Target target)
		{
			if ((fireEngine.m_State & FireEngineFlags.Empty) != 0)
			{
				return false;
			}
			if (m_OnFireData.HasComponent(target.m_Target))
			{
				fireEngine.m_State |= FireEngineFlags.Extinguishing;
				if (!isStopped)
				{
					StopVehicle(jobIndex, entity, ref currentLaneData);
				}
				m_CommandBuffer.AddComponent<EffectsUpdated>(jobIndex, entity);
				return true;
			}
			if (m_RescueTargetData.HasComponent(target.m_Target))
			{
				fireEngine.m_State |= FireEngineFlags.Rescueing;
				if (!isStopped)
				{
					StopVehicle(jobIndex, entity, ref currentLaneData);
				}
				m_CommandBuffer.AddComponent<EffectsUpdated>(jobIndex, entity);
				return true;
			}
			return false;
		}

		private void ReturnToDepot(int jobIndex, Entity vehicleEntity, Owner ownerData, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.FireEngine fireEngine, ref Car carData, ref PathOwner pathOwnerData, ref Target targetData)
		{
			serviceDispatches.Clear();
			fireEngine.m_RequestCount = 0;
			fireEngine.m_State &= ~(FireEngineFlags.Extinguishing | FireEngineFlags.Rescueing);
			fireEngine.m_State |= FireEngineFlags.Returning;
			m_CommandBuffer.AddComponent<EffectsUpdated>(jobIndex, vehicleEntity);
			VehicleUtils.SetTarget(ref pathOwnerData, ref targetData, ownerData.m_Owner);
		}

		private void ResetPath(int jobIndex, Entity vehicleEntity, ref Random random, PathInformation pathInformation, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.FireEngine fireEngine, ref Car carData, ref CarCurrentLane currentLane, ref PathOwner pathOwner)
		{
			DynamicBuffer<PathElement> path = m_PathElements[vehicleEntity];
			PathUtils.ResetPath(ref currentLane, path, m_SlaveLaneData, m_OwnerData, m_SubLanes);
			VehicleUtils.ResetParkingLaneStatus(vehicleEntity, ref currentLane, ref pathOwner, path, ref m_EntityLookup, ref m_CurveData, ref m_ParkingLaneData, ref m_CarLaneData, ref m_ConnectionLaneData, ref m_SpawnLocationData, ref m_PrefabRefData, ref m_PrefabSpawnLocationData);
			VehicleUtils.SetParkingCurvePos(vehicleEntity, ref random, currentLane, pathOwner, path, ref m_ParkedCarData, ref m_UnspawnedData, ref m_CurveData, ref m_ParkingLaneData, ref m_ConnectionLaneData, ref m_PrefabRefData, ref m_PrefabObjectGeometryData, ref m_PrefabParkingLaneData, ref m_LaneObjects, ref m_LaneOverlaps, ignoreDriveways: false);
			if ((fireEngine.m_State & FireEngineFlags.Returning) == 0 && fireEngine.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				if (m_FireRescueRequestData.HasComponent(request))
				{
					carData.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad;
				}
				else
				{
					carData.m_Flags &= ~CarFlags.Emergency;
					carData.m_Flags |= CarFlags.StayOnRoad;
				}
			}
			else
			{
				carData.m_Flags &= ~(CarFlags.Emergency | CarFlags.StayOnRoad);
			}
			carData.m_Flags |= CarFlags.UsePublicTransportLanes;
			fireEngine.m_PathElementTime = pathInformation.m_Duration / (float)math.max(1, path.Length);
			m_CommandBuffer.AddComponent<EffectsUpdated>(jobIndex, vehicleEntity);
		}

		private bool IsCloseEnough(Transform transform, ref Target target)
		{
			if (m_TransformData.HasComponent(target.m_Target))
			{
				Transform transform2 = m_TransformData[target.m_Target];
				return math.distance(transform.m_Position, transform2.m_Position) <= 30f;
			}
			return false;
		}

		private bool TryExtinguishFire(Entity vehicleEntity, FireEngineData prefabFireEngineData, ref Game.Vehicles.FireEngine fireEngine, ref Target target)
		{
			if ((fireEngine.m_State & FireEngineFlags.Empty) != 0)
			{
				return false;
			}
			if (m_TransformData.HasComponent(target.m_Target))
			{
				Transform transform = m_TransformData[target.m_Target];
				float extinguishingSpread = prefabFireEngineData.m_ExtinguishingSpread;
				float num = prefabFireEngineData.m_ExtinguishingRate * fireEngine.m_Efficiency;
				float clearRate = fireEngine.m_Efficiency / math.max(0.001f, prefabFireEngineData.m_DestroyedClearDuration);
				ObjectExtinguishIterator iterator = new ObjectExtinguishIterator
				{
					m_Bounds = new Bounds3(transform.m_Position - extinguishingSpread, transform.m_Position + extinguishingSpread),
					m_Position = transform.m_Position,
					m_Spread = extinguishingSpread,
					m_ExtinguishRate = num,
					m_ClearRate = clearRate,
					m_Vehicle = vehicleEntity,
					m_Target = target.m_Target,
					m_TransformData = m_TransformData,
					m_OnFireData = m_OnFireData,
					m_DestroyedData = m_DestroyedData,
					m_RescueTargetData = m_RescueTargetData,
					m_FireRescueRequestData = m_FireRescueRequestData,
					m_BuildingData = m_BuildingData,
					m_PrefabRefData = m_PrefabRefData,
					m_StructuralIntegrityData = m_StructuralIntegrityData,
					m_ExtinguishingQueue = m_ExtinguishingQueue
				};
				if (m_OnFireData.HasComponent(target.m_Target) || m_RescueTargetData.HasComponent(target.m_Target))
				{
					iterator.TryExtinguish(target.m_Target);
				}
				m_ObjectSearchTree.Iterate(ref iterator);
				if (iterator.m_ExtinguishResult != Entity.Null)
				{
					float num2 = 4f / 15f;
					fireEngine.m_ExtinguishingAmount = math.max(0f, fireEngine.m_ExtinguishingAmount - num * num2);
					if (fireEngine.m_ExtinguishingAmount == 0f && prefabFireEngineData.m_ExtinguishingCapacity != 0f)
					{
						fireEngine.m_State |= FireEngineFlags.Empty;
					}
					return true;
				}
				if (iterator.m_ClearResult != Entity.Null)
				{
					return true;
				}
			}
			return false;
		}

		private void TryAddRequests(Entity vehicleEntity, FireEngineData prefabFireEngineData, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.FireEngine fireEngine, ref Target target)
		{
			if (fireEngine.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				if (m_FireRescueRequestData.HasComponent(request) && m_TransformData.HasComponent(target.m_Target))
				{
					Transform transform = m_TransformData[target.m_Target];
					float extinguishingSpread = prefabFireEngineData.m_ExtinguishingSpread;
					ObjectRequestIterator iterator = new ObjectRequestIterator
					{
						m_Bounds = new Bounds3(transform.m_Position - extinguishingSpread, transform.m_Position + extinguishingSpread),
						m_Position = transform.m_Position,
						m_Spread = extinguishingSpread,
						m_Vehicle = vehicleEntity,
						m_Request = request,
						m_TransformData = m_TransformData,
						m_OnFireData = m_OnFireData,
						m_RescueTargetData = m_RescueTargetData,
						m_FireRescueRequestData = m_FireRescueRequestData,
						m_ExtinguishingQueue = m_ExtinguishingQueue
					};
					m_ObjectSearchTree.Iterate(ref iterator);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct FireExtinguishingJob : IJob
	{
		public ComponentLookup<OnFire> m_OnFireData;

		public ComponentLookup<RescueTarget> m_RescueTargetData;

		public ComponentLookup<Damaged> m_DamagedData;

		public ComponentLookup<Destroyed> m_DestroyedData;

		public NativeQueue<FireExtinguishing> m_ExtinguishingQueue;

		public void Execute()
		{
			FireExtinguishing item;
			while (m_ExtinguishingQueue.TryDequeue(out item))
			{
				if (item.m_Request != Entity.Null)
				{
					if (m_OnFireData.HasComponent(item.m_Target))
					{
						OnFire value = m_OnFireData[item.m_Target];
						value.m_RescueRequest = item.m_Request;
						m_OnFireData[item.m_Target] = value;
					}
					if (m_RescueTargetData.HasComponent(item.m_Target))
					{
						RescueTarget value2 = m_RescueTargetData[item.m_Target];
						value2.m_Request = item.m_Request;
						m_RescueTargetData[item.m_Target] = value2;
					}
				}
				if (item.m_FireIntensityDelta != 0f && m_OnFireData.HasComponent(item.m_Target))
				{
					OnFire value3 = m_OnFireData[item.m_Target];
					value3.m_Intensity = math.max(0f, value3.m_Intensity + item.m_FireIntensityDelta);
					m_OnFireData[item.m_Target] = value3;
				}
				if (item.m_WaterDamageDelta != 0f && m_DamagedData.HasComponent(item.m_Target))
				{
					Damaged value4 = m_DamagedData[item.m_Target];
					if (value4.m_Damage.z < 0.5f)
					{
						value4.m_Damage.z = math.min(0.5f, value4.m_Damage.z + item.m_WaterDamageDelta);
						m_DamagedData[item.m_Target] = value4;
					}
				}
				if (item.m_DestroyedClearDelta != 0f && m_DestroyedData.HasComponent(item.m_Target))
				{
					Destroyed value5 = m_DestroyedData[item.m_Target];
					value5.m_Cleared = math.min(1f, value5.m_Cleared + item.m_DestroyedClearDelta);
					m_DestroyedData[item.m_Target] = value5;
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
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Stopped> __Game_Objects_Stopped_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Vehicles.FireEngine> __Game_Vehicles_FireEngine_RW_ComponentTypeHandle;

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
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SlaveLane> __Game_Net_SlaveLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<GarageLane> __Game_Net_GarageLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<FireEngineData> __Game_Prefabs_FireEngineData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> __Game_Simulation_FireRescueRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OnFire> __Game_Events_OnFire_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Owner> __Game_Common_Owner_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<RescueTarget> __Game_Buildings_RescueTarget_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Building> __Game_Buildings_Building_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.FireStation> __Game_Buildings_FireStation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Unspawned> __Game_Objects_Unspawned_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RW_BufferLookup;

		public ComponentLookup<OnFire> __Game_Events_OnFire_RW_ComponentLookup;

		public ComponentLookup<RescueTarget> __Game_Buildings_RescueTarget_RW_ComponentLookup;

		public ComponentLookup<Damaged> __Game_Objects_Damaged_RW_ComponentLookup;

		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Objects_Stopped_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stopped>(isReadOnly: true);
			__Game_Vehicles_FireEngine_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.FireEngine>();
			__Game_Vehicles_Car_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Car>();
			__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarCurrentLane>();
			__Game_Common_Target_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Target>();
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<CarNavigationLane>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_GarageLane_RO_ComponentLookup = state.GetComponentLookup<GarageLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_FireEngineData_RO_ComponentLookup = state.GetComponentLookup<FireEngineData>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Simulation_FireRescueRequest_RO_ComponentLookup = state.GetComponentLookup<FireRescueRequest>(isReadOnly: true);
			__Game_Events_OnFire_RO_ComponentLookup = state.GetComponentLookup<OnFire>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Buildings_RescueTarget_RO_ComponentLookup = state.GetComponentLookup<RescueTarget>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_FireStation_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.FireStation>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentLookup = state.GetComponentLookup<Unspawned>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
			__Game_Pathfind_PathElement_RW_BufferLookup = state.GetBufferLookup<PathElement>();
			__Game_Events_OnFire_RW_ComponentLookup = state.GetComponentLookup<OnFire>();
			__Game_Buildings_RescueTarget_RW_ComponentLookup = state.GetComponentLookup<RescueTarget>();
			__Game_Objects_Damaged_RW_ComponentLookup = state.GetComponentLookup<Damaged>();
			__Game_Common_Destroyed_RW_ComponentLookup = state.GetComponentLookup<Destroyed>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private EntityQuery m_VehicleQuery;

	private EntityQuery m_ConfigQuery;

	private EntityArchetype m_FireRescueRequestArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private ComponentTypeSet m_MovingToParkedCarRemoveTypes;

	private ComponentTypeSet m_MovingToParkedAddTypes;

	private EventHelpers.StructuralIntegrityData m_StructuralIntegrityData;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 4;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadWrite<CarCurrentLane>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadWrite<Game.Vehicles.FireEngine>(), ComponentType.ReadWrite<Target>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>(), ComponentType.Exclude<OutOfControl>());
		m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<FireConfigurationData>());
		m_FireRescueRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<FireRescueRequest>(), ComponentType.ReadWrite<RequestGroup>());
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
		m_StructuralIntegrityData = new EventHelpers.StructuralIntegrityData(this);
		RequireForUpdate(m_VehicleQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		FireConfigurationData singleton = m_ConfigQuery.GetSingleton<FireConfigurationData>();
		NativeQueue<FireExtinguishing> extinguishingQueue = new NativeQueue<FireExtinguishing>(Allocator.TempJob);
		m_StructuralIntegrityData.Update(this, singleton);
		JobHandle dependencies;
		FireEngineTickJob jobData = new FireEngineTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StoppedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Stopped_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_FireEngineType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_FireEngine_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Car_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarageLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_GarageLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabFireEngineData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_FireEngineData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FireRescueRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_FireRescueRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OnFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_OnFire_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RescueTargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_RescueTarget_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FireStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_FireStation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnspawnedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneOverlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_FireRescueRequestArchetype = m_FireRescueRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_MovingToParkedCarRemoveTypes = m_MovingToParkedCarRemoveTypes,
			m_MovingToParkedAddTypes = m_MovingToParkedAddTypes,
			m_SimulationFrameIndex = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_StructuralIntegrityData = m_StructuralIntegrityData,
			m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
			m_ExtinguishingQueue = extinguishingQueue.AsParallelWriter()
		};
		FireExtinguishingJob jobData2 = new FireExtinguishingJob
		{
			m_OnFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_OnFire_RW_ComponentLookup, ref base.CheckedStateRef),
			m_RescueTargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_RescueTarget_RW_ComponentLookup, ref base.CheckedStateRef),
			m_DamagedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RW_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ExtinguishingQueue = extinguishingQueue
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_VehicleQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		extinguishingQueue.Dispose(jobHandle2);
		m_ObjectSearchSystem.AddStaticSearchTreeReader(jobHandle);
		m_PathfindSetupSystem.AddQueueWriter(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle2;
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
	public FireEngineAISystem()
	{
	}
}
