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
public class FireAircraftAISystem : GameSystemBase
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
	private struct FireAircraftTickJob : IJobChunk
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
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		public ComponentTypeHandle<Game.Vehicles.FireEngine> m_FireEngineType;

		public ComponentTypeHandle<Aircraft> m_AircraftType;

		public ComponentTypeHandle<AircraftCurrentLane> m_CurrentLaneType;

		public ComponentTypeHandle<Target> m_TargetType;

		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		public BufferTypeHandle<AircraftNavigationLane> m_AircraftNavigationLaneType;

		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<Controller> m_ControllerData;

		[ReadOnly]
		public ComponentLookup<HelicopterData> m_PrefabHelicopterData;

		[ReadOnly]
		public ComponentLookup<FireEngineData> m_PrefabFireEngineData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> m_FireRescueRequestData;

		[ReadOnly]
		public ComponentLookup<OnFire> m_OnFireData;

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
		public BufferLookup<LayoutElement> m_LayoutElements;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Blocker> m_BlockerData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public EntityArchetype m_FireRescueRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedAircraftRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedAddTypes;

		[ReadOnly]
		public uint m_SimulationFrameIndex;

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
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<PathInformation> nativeArray4 = chunk.GetNativeArray(ref m_PathInformationType);
			NativeArray<AircraftCurrentLane> nativeArray5 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<Game.Vehicles.FireEngine> nativeArray6 = chunk.GetNativeArray(ref m_FireEngineType);
			NativeArray<Aircraft> nativeArray7 = chunk.GetNativeArray(ref m_AircraftType);
			NativeArray<Target> nativeArray8 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<PathOwner> nativeArray9 = chunk.GetNativeArray(ref m_PathOwnerType);
			BufferAccessor<AircraftNavigationLane> bufferAccessor = chunk.GetBufferAccessor(ref m_AircraftNavigationLaneType);
			BufferAccessor<ServiceDispatch> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Owner owner = nativeArray2[i];
				PrefabRef prefabRef = nativeArray3[i];
				PathInformation pathInformation = nativeArray4[i];
				Game.Vehicles.FireEngine fireEngine = nativeArray6[i];
				Aircraft aircraft = nativeArray7[i];
				AircraftCurrentLane currentLane = nativeArray5[i];
				PathOwner pathOwner = nativeArray9[i];
				Target target = nativeArray8[i];
				DynamicBuffer<AircraftNavigationLane> navigationLanes = bufferAccessor[i];
				DynamicBuffer<ServiceDispatch> serviceDispatches = bufferAccessor2[i];
				VehicleUtils.CheckUnspawned(unfilteredChunkIndex, entity, currentLane, isUnspawned, m_CommandBuffer);
				Tick(unfilteredChunkIndex, entity, owner, prefabRef, pathInformation, navigationLanes, serviceDispatches, ref fireEngine, ref aircraft, ref currentLane, ref pathOwner, ref target);
				nativeArray6[i] = fireEngine;
				nativeArray7[i] = aircraft;
				nativeArray5[i] = currentLane;
				nativeArray9[i] = pathOwner;
				nativeArray8[i] = target;
			}
		}

		private void Tick(int jobIndex, Entity vehicleEntity, Owner owner, PrefabRef prefabRef, PathInformation pathInformation, DynamicBuffer<AircraftNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.FireEngine fireEngine, ref Aircraft aircraft, ref AircraftCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if (VehicleUtils.ResetUpdatedPath(ref pathOwner))
			{
				ResetPath(jobIndex, vehicleEntity, pathInformation, serviceDispatches, ref fireEngine, ref aircraft, ref currentLane);
			}
			FireEngineData fireEngineData = m_PrefabFireEngineData[prefabRef.m_Prefab];
			if (VehicleUtils.IsStuck(pathOwner))
			{
				Blocker blocker = m_BlockerData[vehicleEntity];
				bool num = m_ParkedCarData.HasComponent(blocker.m_Blocker);
				if (num)
				{
					Entity entity = blocker.m_Blocker;
					if (m_ControllerData.TryGetComponent(entity, out var componentData))
					{
						entity = componentData.m_Controller;
					}
					m_LayoutElements.TryGetBuffer(entity, out var bufferData);
					VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, entity, bufferData);
				}
				if (num || blocker.m_Blocker == Entity.Null)
				{
					pathOwner.m_State &= ~PathFlags.Stuck;
					m_BlockerData[vehicleEntity] = default(Blocker);
				}
			}
			if (!m_EntityLookup.Exists(target.m_Target) || VehicleUtils.PathfindFailed(pathOwner))
			{
				if (VehicleUtils.IsStuck(pathOwner) || (fireEngine.m_State & FireEngineFlags.Returning) != 0)
				{
					m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					return;
				}
				ReturnToDepot(jobIndex, vehicleEntity, owner, serviceDispatches, ref fireEngine, ref aircraft, ref pathOwner, ref target);
			}
			else if (VehicleUtils.PathEndReached(currentLane))
			{
				if ((fireEngine.m_State & FireEngineFlags.Returning) != 0)
				{
					if (VehicleUtils.ParkingSpaceReached(currentLane, pathOwner))
					{
						ParkAircraft(jobIndex, vehicleEntity, owner, fireEngineData, ref aircraft, ref fireEngine, ref currentLane);
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					}
					return;
				}
				if ((fireEngine.m_State & (FireEngineFlags.Extinguishing | FireEngineFlags.Rescueing)) == 0 && !BeginExtinguishing(jobIndex, vehicleEntity, ref fireEngine, target))
				{
					CheckServiceDispatches(vehicleEntity, serviceDispatches, fireEngineData, ref fireEngine, ref pathOwner);
					if ((fireEngine.m_State & (FireEngineFlags.Empty | FireEngineFlags.Disabled)) != 0 || !SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, ref fireEngine, ref aircraft, ref currentLane, ref pathOwner, ref target))
					{
						ReturnToDepot(jobIndex, vehicleEntity, owner, serviceDispatches, ref fireEngine, ref aircraft, ref pathOwner, ref target);
					}
				}
			}
			else if ((currentLane.m_LaneFlags & AircraftLaneFlags.EndOfPath) != 0 && (fireEngine.m_State & (FireEngineFlags.Returning | FireEngineFlags.Extinguishing | FireEngineFlags.Rescueing)) == 0 && !CheckExtinguishing(jobIndex, vehicleEntity, ref fireEngine, target))
			{
				CheckServiceDispatches(vehicleEntity, serviceDispatches, fireEngineData, ref fireEngine, ref pathOwner);
				if ((fireEngine.m_State & (FireEngineFlags.Empty | FireEngineFlags.Disabled)) != 0 || !SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, ref fireEngine, ref aircraft, ref currentLane, ref pathOwner, ref target))
				{
					ReturnToDepot(jobIndex, vehicleEntity, owner, serviceDispatches, ref fireEngine, ref aircraft, ref pathOwner, ref target);
				}
			}
			if (fireEngineData.m_ExtinguishingCapacity != 0f && fireEngine.m_RequestCount <= 1)
			{
				if (fireEngine.m_RequestCount == 1 && m_OnFireData.TryGetComponent(target.m_Target, out var componentData2) && componentData2.m_Intensity > 0f)
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
				if (!TryExtinguishFire(vehicleEntity, fireEngineData, ref fireEngine, ref target) && ((fireEngine.m_State & (FireEngineFlags.Empty | FireEngineFlags.Disabled)) != 0 || !SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, ref fireEngine, ref aircraft, ref currentLane, ref pathOwner, ref target)))
				{
					ReturnToDepot(jobIndex, vehicleEntity, owner, serviceDispatches, ref fireEngine, ref aircraft, ref pathOwner, ref target);
				}
			}
			else if ((fireEngine.m_State & (FireEngineFlags.Returning | FireEngineFlags.Empty | FireEngineFlags.Disabled)) == FireEngineFlags.Returning)
			{
				SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, ref fireEngine, ref aircraft, ref currentLane, ref pathOwner, ref target);
			}
			if ((aircraft.m_Flags & AircraftFlags.Emergency) != 0)
			{
				TryAddRequests(vehicleEntity, fireEngineData, serviceDispatches, ref fireEngine, ref target);
			}
			if ((fireEngine.m_State & (FireEngineFlags.Extinguishing | FireEngineFlags.Rescueing)) == 0)
			{
				if (VehicleUtils.RequireNewPath(pathOwner))
				{
					FindNewPath(vehicleEntity, prefabRef, ref fireEngine, ref currentLane, ref pathOwner, ref target);
				}
				else if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Stuck)) == 0)
				{
					CheckParkingSpace(ref aircraft, ref currentLane, ref pathOwner);
				}
			}
		}

		private void CheckParkingSpace(ref Aircraft aircraft, ref AircraftCurrentLane currentLane, ref PathOwner pathOwner)
		{
			if ((currentLane.m_LaneFlags & AircraftLaneFlags.EndOfPath) == 0 || !m_SpawnLocationData.TryGetComponent(currentLane.m_Lane, out var componentData))
			{
				return;
			}
			if ((componentData.m_Flags & SpawnLocationFlags.ParkedVehicle) != 0)
			{
				if ((aircraft.m_Flags & AircraftFlags.IgnoreParkedVehicle) == 0)
				{
					aircraft.m_Flags |= AircraftFlags.IgnoreParkedVehicle;
					pathOwner.m_State |= PathFlags.Obsolete;
				}
			}
			else
			{
				aircraft.m_Flags &= ~AircraftFlags.IgnoreParkedVehicle;
			}
		}

		private void ParkAircraft(int jobIndex, Entity entity, Owner owner, FireEngineData fireEngineData, ref Aircraft aircraft, ref Game.Vehicles.FireEngine fireEngine, ref AircraftCurrentLane currentLane)
		{
			aircraft.m_Flags &= ~(AircraftFlags.Emergency | AircraftFlags.IgnoreParkedVehicle);
			fireEngine.m_State = (FireEngineFlags)0u;
			fireEngine.m_ExtinguishingAmount = fireEngineData.m_ExtinguishingCapacity;
			if (m_FireStationData.TryGetComponent(owner.m_Owner, out var componentData))
			{
				if ((componentData.m_Flags & FireStationFlags.HasAvailableFireHelicopters) == 0)
				{
					fireEngine.m_State |= FireEngineFlags.Disabled;
				}
				if ((componentData.m_Flags & FireStationFlags.DisasterResponseAvailable) != 0)
				{
					fireEngine.m_State |= FireEngineFlags.DisasterResponse;
				}
			}
			m_CommandBuffer.RemoveComponent(jobIndex, entity, in m_MovingToParkedAircraftRemoveTypes);
			m_CommandBuffer.AddComponent(jobIndex, entity, in m_MovingToParkedAddTypes);
			m_CommandBuffer.SetComponent(jobIndex, entity, new ParkedCar(currentLane.m_Lane, currentLane.m_CurvePosition.x));
			if (m_SpawnLocationData.HasComponent(currentLane.m_Lane))
			{
				m_CommandBuffer.AddComponent<PathfindUpdated>(jobIndex, currentLane.m_Lane);
			}
			m_CommandBuffer.AddComponent(jobIndex, entity, new FixParkingLocation(Entity.Null, entity));
		}

		private void FindNewPath(Entity vehicleEntity, PrefabRef prefabRef, ref Game.Vehicles.FireEngine fireEngine, ref AircraftCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			HelicopterData helicopterData = m_PrefabHelicopterData[prefabRef.m_Prefab];
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = helicopterData.m_FlyingMaxSpeed,
				m_WalkSpeed = 5.555556f,
				m_Methods = (PathMethod.Road | PathMethod.Flying),
				m_IgnoredRules = (RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
			};
			SetupQueueTarget origin = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Methods = (PathMethod.Road | PathMethod.Flying),
				m_RoadTypes = RoadTypes.Helicopter,
				m_FlyingTypes = RoadTypes.Helicopter
			};
			SetupQueueTarget destination = new SetupQueueTarget
			{
				m_Type = SetupTargetType.CurrentLocation,
				m_Entity = target.m_Target
			};
			if ((fireEngine.m_State & FireEngineFlags.Returning) == 0)
			{
				parameters.m_Weights = new PathfindWeights(1f, 0f, 0f, 0f);
				parameters.m_IgnoredRules |= RuleFlags.ForbidHeavyTraffic;
				destination.m_Value2 = 30f;
				destination.m_Methods = PathMethod.Flying;
				destination.m_FlyingTypes = RoadTypes.Helicopter;
			}
			else
			{
				parameters.m_Weights = new PathfindWeights(1f, 1f, 1f, 1f);
				destination.m_Methods = PathMethod.Road;
				destination.m_RoadTypes = RoadTypes.Helicopter;
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
				if (m_EntityLookup.Exists(fireRescueRequest.m_Target) && fireRescueRequest.m_Priority > num)
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
				if ((m_SimulationFrameIndex & (num - 1)) == 10)
				{
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_FireRescueRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e, new FireRescueRequest(entity, 1f, FireRescueRequestType.Fire));
					m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(4u));
				}
			}
		}

		private bool SelectNextDispatch(int jobIndex, Entity vehicleEntity, DynamicBuffer<AircraftNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.FireEngine fireEngine, ref Aircraft aircraft, ref AircraftCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
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
				aircraft.m_Flags &= ~AircraftFlags.IgnoreParkedVehicle;
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
						if (PathUtils.TryAppendPath(ref currentLane, navigationLanes, dynamicBuffer, appendPath))
						{
							fireEngine.m_PathElementTime = num / (float)math.max(1, dynamicBuffer.Length);
							target.m_Target = entity;
							VehicleUtils.ClearEndOfPath(ref currentLane, navigationLanes);
							aircraft.m_Flags |= AircraftFlags.Emergency | AircraftFlags.StayMidAir;
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

		private bool BeginExtinguishing(int jobIndex, Entity vehicleEntity, ref Game.Vehicles.FireEngine fireEngine, Target target)
		{
			if ((fireEngine.m_State & FireEngineFlags.Empty) != 0)
			{
				return false;
			}
			if (m_OnFireData.HasComponent(target.m_Target))
			{
				fireEngine.m_State |= FireEngineFlags.Extinguishing;
				m_CommandBuffer.AddComponent<EffectsUpdated>(jobIndex, vehicleEntity);
				return true;
			}
			if (m_RescueTargetData.HasComponent(target.m_Target))
			{
				fireEngine.m_State |= FireEngineFlags.Rescueing;
				return true;
			}
			return false;
		}

		private bool CheckExtinguishing(int jobIndex, Entity vehicleEntity, ref Game.Vehicles.FireEngine fireEngine, Target target)
		{
			if ((fireEngine.m_State & FireEngineFlags.Empty) != 0)
			{
				return false;
			}
			if (m_OnFireData.HasComponent(target.m_Target))
			{
				return true;
			}
			if (m_RescueTargetData.HasComponent(target.m_Target))
			{
				return true;
			}
			return false;
		}

		private void ReturnToDepot(int jobIndex, Entity vehicleEntity, Owner ownerData, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.FireEngine fireEngine, ref Aircraft aircraft, ref PathOwner pathOwnerData, ref Target targetData)
		{
			serviceDispatches.Clear();
			fireEngine.m_RequestCount = 0;
			fireEngine.m_State &= ~(FireEngineFlags.Extinguishing | FireEngineFlags.Rescueing);
			fireEngine.m_State |= FireEngineFlags.Returning;
			aircraft.m_Flags &= ~AircraftFlags.IgnoreParkedVehicle;
			m_CommandBuffer.AddComponent<EffectsUpdated>(jobIndex, vehicleEntity);
			VehicleUtils.SetTarget(ref pathOwnerData, ref targetData, ownerData.m_Owner);
		}

		private void ResetPath(int jobIndex, Entity vehicleEntity, PathInformation pathInformation, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.FireEngine fireEngine, ref Aircraft aircraft, ref AircraftCurrentLane currentLane)
		{
			DynamicBuffer<PathElement> path = m_PathElements[vehicleEntity];
			PathUtils.ResetPath(ref currentLane, path);
			if ((fireEngine.m_State & FireEngineFlags.Returning) == 0 && fireEngine.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				if (m_FireRescueRequestData.HasComponent(request))
				{
					aircraft.m_Flags |= AircraftFlags.Emergency | AircraftFlags.StayMidAir;
				}
				else
				{
					aircraft.m_Flags &= ~AircraftFlags.Emergency;
					aircraft.m_Flags |= AircraftFlags.StayMidAir;
				}
			}
			else
			{
				aircraft.m_Flags &= ~(AircraftFlags.StayOnTaxiway | AircraftFlags.Emergency | AircraftFlags.StayMidAir);
			}
			fireEngine.m_PathElementTime = pathInformation.m_Duration / (float)math.max(1, path.Length);
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
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Vehicles.FireEngine> __Game_Vehicles_FireEngine_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Aircraft> __Game_Vehicles_Aircraft_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Target> __Game_Common_Target_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;

		public BufferTypeHandle<AircraftNavigationLane> __Game_Vehicles_AircraftNavigationLane_RW_BufferTypeHandle;

		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Controller> __Game_Vehicles_Controller_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HelicopterData> __Game_Prefabs_HelicopterData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<FireEngineData> __Game_Prefabs_FireEngineData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<FireRescueRequest> __Game_Simulation_FireRescueRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OnFire> __Game_Events_OnFire_RO_ComponentLookup;

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
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		public ComponentLookup<Blocker> __Game_Vehicles_Blocker_RW_ComponentLookup;

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
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Vehicles_FireEngine_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.FireEngine>();
			__Game_Vehicles_Aircraft_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Aircraft>();
			__Game_Vehicles_AircraftCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AircraftCurrentLane>();
			__Game_Common_Target_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Target>();
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Vehicles_AircraftNavigationLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<AircraftNavigationLane>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Prefabs_HelicopterData_RO_ComponentLookup = state.GetComponentLookup<HelicopterData>(isReadOnly: true);
			__Game_Prefabs_FireEngineData_RO_ComponentLookup = state.GetComponentLookup<FireEngineData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Simulation_FireRescueRequest_RO_ComponentLookup = state.GetComponentLookup<FireRescueRequest>(isReadOnly: true);
			__Game_Events_OnFire_RO_ComponentLookup = state.GetComponentLookup<OnFire>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Buildings_RescueTarget_RO_ComponentLookup = state.GetComponentLookup<RescueTarget>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentLookup = state.GetComponentLookup<Building>(isReadOnly: true);
			__Game_Buildings_FireStation_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.FireStation>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Vehicles_Blocker_RW_ComponentLookup = state.GetComponentLookup<Blocker>();
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

	private ComponentTypeSet m_MovingToParkedAircraftRemoveTypes;

	private ComponentTypeSet m_MovingToParkedAddTypes;

	private EventHelpers.StructuralIntegrityData m_StructuralIntegrityData;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 10;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_ObjectSearchSystem = base.World.GetOrCreateSystemManaged<Game.Objects.SearchSystem>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadWrite<AircraftCurrentLane>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadWrite<Game.Vehicles.FireEngine>(), ComponentType.ReadWrite<Target>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>(), ComponentType.Exclude<OutOfControl>());
		m_ConfigQuery = GetEntityQuery(ComponentType.ReadOnly<FireConfigurationData>());
		m_FireRescueRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<FireRescueRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_HandleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(), ComponentType.ReadWrite<Game.Common.Event>());
		m_MovingToParkedAircraftRemoveTypes = new ComponentTypeSet(new ComponentType[12]
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
		FireAircraftTickJob jobData = new FireAircraftTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_FireEngineType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_FireEngine_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AircraftType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Aircraft_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AircraftNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftNavigationLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabHelicopterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HelicopterData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabFireEngineData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_FireEngineData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FireRescueRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_FireRescueRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OnFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_OnFire_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_RescueTargetData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_RescueTarget_RO_ComponentLookup, ref base.CheckedStateRef),
			m_BuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentLookup, ref base.CheckedStateRef),
			m_FireStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_FireStation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_BlockerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Blocker_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_FireRescueRequestArchetype = m_FireRescueRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_MovingToParkedAircraftRemoveTypes = m_MovingToParkedAircraftRemoveTypes,
			m_MovingToParkedAddTypes = m_MovingToParkedAddTypes,
			m_SimulationFrameIndex = m_SimulationSystem.frameIndex,
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
	public FireAircraftAISystem()
	{
	}
}
