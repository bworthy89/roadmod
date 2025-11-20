using System.Runtime.CompilerServices;
using Colossal.Mathematics;
using Game.Buildings;
using Game.City;
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
using UnityEngine;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class MaintenanceVehicleAISystem : GameSystemBase
{
	private struct MaintenanceAction
	{
		public MaintenanceActionType m_Type;

		public Entity m_Vehicle;

		public Entity m_Consumer;

		public Entity m_Request;

		public int m_VehicleCapacity;

		public int m_ConsumerCapacity;

		public int m_MaxMaintenanceAmount;

		public CarFlags m_WorkingFlags;
	}

	private enum MaintenanceActionType
	{
		AddRequest,
		ParkMaintenance,
		RoadMaintenance,
		RepairVehicle,
		ClearLane,
		BumpDispatchIndex
	}

	[BurstCompile]
	private struct MaintenanceVehicleTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Owner> m_OwnerType;

		[ReadOnly]
		public ComponentTypeHandle<Unspawned> m_UnspawnedType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> m_PathInformationType;

		[ReadOnly]
		public ComponentTypeHandle<Stopped> m_StoppedType;

		public ComponentTypeHandle<Game.Vehicles.MaintenanceVehicle> m_MaintenanceVehicleType;

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
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Unspawned> m_UnspawnedData;

		[ReadOnly]
		public ComponentLookup<Vehicle> m_VehicleData;

		[ReadOnly]
		public ComponentLookup<Destroyed> m_DestroyedData;

		[ReadOnly]
		public ComponentLookup<Damaged> m_DamagedData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		[ReadOnly]
		public ComponentLookup<NetCondition> m_NetConditionData;

		[ReadOnly]
		public ComponentLookup<LaneCondition> m_LaneConditionData;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> m_CarLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.PedestrianLane> m_PedestrianLaneData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<SlaveLane> m_SlaveLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<GarageLane> m_GarageLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<InvolvedInAccident> m_InvolvedInAccidentData;

		[ReadOnly]
		public ComponentLookup<AccidentSite> m_AccidentSiteData;

		[ReadOnly]
		public ComponentLookup<OnFire> m_OnFireData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> m_ParkData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.MaintenanceDepot> m_MaintenanceDepotData;

		[ReadOnly]
		public ComponentLookup<CarData> m_PrefabCarData;

		[ReadOnly]
		public ComponentLookup<MaintenanceVehicleData> m_PrefabMaintenanceVehicleData;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public ComponentLookup<ParkData> m_PrefabParkData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<MaintenanceRequest> m_MaintenanceRequestData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		[ReadOnly]
		public BufferLookup<LaneOverlap> m_LaneOverlaps;

		[ReadOnly]
		public BufferLookup<TargetElement> m_TargetElements;

		[NativeDisableParallelForRestriction]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public bool m_LeftHandTraffic;

		[ReadOnly]
		public uint m_SimulationFrameIndex;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_MaintenanceRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedCarRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedAddTypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public NativeQueue<MaintenanceAction>.ParallelWriter m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Game.Objects.Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<PathInformation> nativeArray5 = chunk.GetNativeArray(ref m_PathInformationType);
			NativeArray<CarCurrentLane> nativeArray6 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<Game.Vehicles.MaintenanceVehicle> nativeArray7 = chunk.GetNativeArray(ref m_MaintenanceVehicleType);
			NativeArray<Car> nativeArray8 = chunk.GetNativeArray(ref m_CarType);
			NativeArray<Target> nativeArray9 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<PathOwner> nativeArray10 = chunk.GetNativeArray(ref m_PathOwnerType);
			BufferAccessor<CarNavigationLane> bufferAccessor = chunk.GetBufferAccessor(ref m_CarNavigationLaneType);
			BufferAccessor<ServiceDispatch> bufferAccessor2 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			bool isStopped = chunk.Has(ref m_StoppedType);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Owner owner = nativeArray2[i];
				Game.Objects.Transform transform = nativeArray3[i];
				PrefabRef prefabRef = nativeArray4[i];
				PathInformation pathInformation = nativeArray5[i];
				Game.Vehicles.MaintenanceVehicle maintenanceVehicle = nativeArray7[i];
				Car car = nativeArray8[i];
				CarCurrentLane currentLane = nativeArray6[i];
				PathOwner pathOwner = nativeArray10[i];
				Target target = nativeArray9[i];
				DynamicBuffer<CarNavigationLane> navigationLanes = bufferAccessor[i];
				DynamicBuffer<ServiceDispatch> serviceDispatches = bufferAccessor2[i];
				VehicleUtils.CheckUnspawned(unfilteredChunkIndex, entity, currentLane, isUnspawned, m_CommandBuffer);
				Tick(unfilteredChunkIndex, entity, owner, transform, prefabRef, pathInformation, navigationLanes, serviceDispatches, isStopped, ref random, ref maintenanceVehicle, ref car, ref currentLane, ref pathOwner, ref target);
				nativeArray7[i] = maintenanceVehicle;
				nativeArray8[i] = car;
				nativeArray6[i] = currentLane;
				nativeArray10[i] = pathOwner;
				nativeArray9[i] = target;
			}
		}

		private void Tick(int jobIndex, Entity vehicleEntity, Owner owner, Game.Objects.Transform transform, PrefabRef prefabRef, PathInformation pathInformation, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, bool isStopped, ref Unity.Mathematics.Random random, ref Game.Vehicles.MaintenanceVehicle maintenanceVehicle, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			MaintenanceVehicleData prefabMaintenanceVehicleData = m_PrefabMaintenanceVehicleData[prefabRef.m_Prefab];
			prefabMaintenanceVehicleData.m_MaintenanceCapacity = Mathf.CeilToInt((float)prefabMaintenanceVehicleData.m_MaintenanceCapacity * maintenanceVehicle.m_Efficiency);
			if (VehicleUtils.ResetUpdatedPath(ref pathOwner))
			{
				ResetPath(jobIndex, vehicleEntity, pathInformation, serviceDispatches, prefabMaintenanceVehicleData, ref random, ref maintenanceVehicle, ref car, ref currentLane, ref pathOwner);
			}
			if (!m_EntityLookup.Exists(target.m_Target) || VehicleUtils.PathfindFailed(pathOwner))
			{
				if (VehicleUtils.IsStuck(pathOwner) || (maintenanceVehicle.m_State & MaintenanceVehicleFlags.Returning) != 0)
				{
					m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					return;
				}
				ReturnToDepot(jobIndex, vehicleEntity, owner, serviceDispatches, ref car, ref maintenanceVehicle, ref pathOwner, ref target);
			}
			else if (VehicleUtils.PathEndReached(currentLane))
			{
				if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.Returning) != 0)
				{
					m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					return;
				}
				if ((maintenanceVehicle.m_State & (MaintenanceVehicleFlags.TryWork | MaintenanceVehicleFlags.Working)) != MaintenanceVehicleFlags.TryWork)
				{
					if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.EdgeTarget) != 0)
					{
						TryMaintain(vehicleEntity, prefabMaintenanceVehicleData, ref car, ref maintenanceVehicle, ref currentLane);
					}
					else if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.TransformTarget) != 0)
					{
						if (IsSecureSite(ref target))
						{
							TryMaintain(vehicleEntity, prefabMaintenanceVehicleData, ref car, ref maintenanceVehicle, ref currentLane, ref target);
						}
						else if (!isStopped)
						{
							StopVehicle(jobIndex, vehicleEntity, ref currentLane);
						}
					}
					else
					{
						maintenanceVehicle.m_State &= ~MaintenanceVehicleFlags.Working;
					}
					return;
				}
				CheckServiceDispatches(vehicleEntity, serviceDispatches, prefabMaintenanceVehicleData, ref maintenanceVehicle, ref pathOwner);
				if (!SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, prefabMaintenanceVehicleData, ref maintenanceVehicle, ref car, ref currentLane, ref pathOwner, ref target))
				{
					ReturnToDepot(jobIndex, vehicleEntity, owner, serviceDispatches, ref car, ref maintenanceVehicle, ref pathOwner, ref target);
				}
			}
			else
			{
				if (VehicleUtils.ParkingSpaceReached(currentLane, pathOwner))
				{
					if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.Returning) != 0)
					{
						ParkCar(jobIndex, vehicleEntity, owner, ref maintenanceVehicle, ref car, ref currentLane);
					}
					else
					{
						m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					}
					return;
				}
				if (VehicleUtils.WaypointReached(currentLane))
				{
					if ((maintenanceVehicle.m_State & (MaintenanceVehicleFlags.TryWork | MaintenanceVehicleFlags.Working | MaintenanceVehicleFlags.ClearingDebris)) != MaintenanceVehicleFlags.TryWork && (maintenanceVehicle.m_State & MaintenanceVehicleFlags.EdgeTarget) != 0)
					{
						TryMaintain(vehicleEntity, prefabMaintenanceVehicleData, ref car, ref maintenanceVehicle, ref currentLane);
					}
					else
					{
						currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.Waypoint;
						maintenanceVehicle.m_State &= ~(MaintenanceVehicleFlags.TryWork | MaintenanceVehicleFlags.Working | MaintenanceVehicleFlags.ClearingDebris);
					}
				}
				else if ((maintenanceVehicle.m_State & (MaintenanceVehicleFlags.TryWork | MaintenanceVehicleFlags.Working)) != 0)
				{
					maintenanceVehicle.m_State &= ~(MaintenanceVehicleFlags.TryWork | MaintenanceVehicleFlags.Working | MaintenanceVehicleFlags.ClearingDebris);
				}
				else if (isStopped)
				{
					StartVehicle(jobIndex, vehicleEntity, ref currentLane);
				}
				else if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.TransformTarget) != 0 && (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.IsBlocked) != 0 && IsCloseEnough(transform, ref target))
				{
					EndNavigation(vehicleEntity, ref currentLane, ref pathOwner, navigationLanes);
				}
			}
			if (maintenanceVehicle.m_Maintained >= prefabMaintenanceVehicleData.m_MaintenanceCapacity)
			{
				maintenanceVehicle.m_State |= MaintenanceVehicleFlags.Full | MaintenanceVehicleFlags.EstimatedFull;
			}
			else if (maintenanceVehicle.m_Maintained + maintenanceVehicle.m_MaintainEstimate >= prefabMaintenanceVehicleData.m_MaintenanceCapacity)
			{
				maintenanceVehicle.m_State |= MaintenanceVehicleFlags.EstimatedFull;
				maintenanceVehicle.m_State &= ~MaintenanceVehicleFlags.Full;
			}
			else
			{
				maintenanceVehicle.m_State &= ~(MaintenanceVehicleFlags.Full | MaintenanceVehicleFlags.EstimatedFull);
			}
			if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.Returning) == 0)
			{
				if ((maintenanceVehicle.m_State & (MaintenanceVehicleFlags.Full | MaintenanceVehicleFlags.Disabled)) != 0)
				{
					if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.TryWork) == 0)
					{
						ReturnToDepot(jobIndex, vehicleEntity, owner, serviceDispatches, ref car, ref maintenanceVehicle, ref pathOwner, ref target);
					}
				}
				else
				{
					CheckMaintenancePresence(ref car, ref maintenanceVehicle, ref currentLane, navigationLanes);
				}
			}
			if ((maintenanceVehicle.m_State & (MaintenanceVehicleFlags.Full | MaintenanceVehicleFlags.Disabled)) == 0)
			{
				CheckServiceDispatches(vehicleEntity, serviceDispatches, prefabMaintenanceVehicleData, ref maintenanceVehicle, ref pathOwner);
				if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.Returning) != 0)
				{
					SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, prefabMaintenanceVehicleData, ref maintenanceVehicle, ref car, ref currentLane, ref pathOwner, ref target);
				}
				if (maintenanceVehicle.m_RequestCount <= 1 && (maintenanceVehicle.m_State & MaintenanceVehicleFlags.EstimatedFull) == 0)
				{
					RequestTargetIfNeeded(jobIndex, vehicleEntity, ref maintenanceVehicle);
				}
			}
			else if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.TryWork) == 0)
			{
				serviceDispatches.Clear();
			}
			if ((maintenanceVehicle.m_State & (MaintenanceVehicleFlags.TryWork | MaintenanceVehicleFlags.Working)) != 0)
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
					FindNewPath(vehicleEntity, prefabRef, prefabMaintenanceVehicleData, ref maintenanceVehicle, ref currentLane, ref pathOwner, ref target);
				}
			}
			else if ((pathOwner.m_State & (PathFlags.Pending | PathFlags.Failed | PathFlags.Stuck)) == 0)
			{
				CheckParkingSpace(vehicleEntity, ref random, ref currentLane, ref pathOwner, navigationLanes);
			}
		}

		private void CheckParkingSpace(Entity entity, ref Unity.Mathematics.Random random, ref CarCurrentLane currentLane, ref PathOwner pathOwner, DynamicBuffer<CarNavigationLane> navigationLanes)
		{
			DynamicBuffer<PathElement> path = m_PathElements[entity];
			ComponentLookup<Blocker> blockerData = default(ComponentLookup<Blocker>);
			VehicleUtils.ValidateParkingSpace(entity, ref random, ref currentLane, ref pathOwner, navigationLanes, path, ref m_ParkedCarData, ref blockerData, ref m_CurveData, ref m_UnspawnedData, ref m_ParkingLaneData, ref m_GarageLaneData, ref m_ConnectionLaneData, ref m_PrefabRefData, ref m_PrefabParkingLaneData, ref m_PrefabObjectGeometryData, ref m_LaneObjects, ref m_LaneOverlaps, ignoreDriveways: false, ignoreDisabled: false, boardingOnly: false);
		}

		private void ParkCar(int jobIndex, Entity entity, Owner owner, ref Game.Vehicles.MaintenanceVehicle maintenanceVehicle, ref Car car, ref CarCurrentLane currentLane)
		{
			car.m_Flags &= ~(CarFlags.Warning | CarFlags.Working | CarFlags.SignalAnimation1 | CarFlags.SignalAnimation2);
			maintenanceVehicle.m_State = (MaintenanceVehicleFlags)0u;
			maintenanceVehicle.m_Maintained = 0;
			if (m_MaintenanceDepotData.TryGetComponent(owner.m_Owner, out var componentData) && (componentData.m_Flags & MaintenanceDepotFlags.HasAvailableVehicles) == 0)
			{
				maintenanceVehicle.m_State |= MaintenanceVehicleFlags.Disabled;
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

		private bool IsSecureSite(ref Target target)
		{
			if (m_OnFireData.HasComponent(target.m_Target))
			{
				return false;
			}
			if (m_InvolvedInAccidentData.HasComponent(target.m_Target))
			{
				InvolvedInAccident involvedInAccident = m_InvolvedInAccidentData[target.m_Target];
				if (m_TargetElements.HasBuffer(involvedInAccident.m_Event))
				{
					DynamicBuffer<TargetElement> dynamicBuffer = m_TargetElements[involvedInAccident.m_Event];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						Entity entity = dynamicBuffer[i].m_Entity;
						if (m_AccidentSiteData.HasComponent(entity))
						{
							return (m_AccidentSiteData[entity].m_Flags & AccidentSiteFlags.Secured) != 0;
						}
					}
				}
			}
			return true;
		}

		private void EndNavigation(Entity vehicleEntity, ref CarCurrentLane currentLane, ref PathOwner pathOwner, DynamicBuffer<CarNavigationLane> navigationLanes)
		{
			currentLane.m_CurvePosition.z = currentLane.m_CurvePosition.y;
			currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.EndOfPath;
			navigationLanes.Clear();
			pathOwner.m_ElementIndex = 0;
			m_PathElements[vehicleEntity].Clear();
		}

		private bool IsCloseEnough(Game.Objects.Transform transform, ref Target target)
		{
			if (m_TransformData.HasComponent(target.m_Target))
			{
				Game.Objects.Transform transform2 = m_TransformData[target.m_Target];
				return math.distance(transform.m_Position, transform2.m_Position) <= 30f;
			}
			return false;
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

		private void FindNewPath(Entity vehicleEntity, PrefabRef prefabRef, MaintenanceVehicleData prefabMaintenanceVehicleData, ref Game.Vehicles.MaintenanceVehicle maintenanceVehicle, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
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
				m_IgnoredRules = VehicleUtils.GetIgnoredPathfindRules(carData)
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
			if ((prefabMaintenanceVehicleData.m_MaintenanceType & (MaintenanceType.Road | MaintenanceType.Snow | MaintenanceType.Vehicle)) != MaintenanceType.None)
			{
				parameters.m_IgnoredRules |= RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic;
			}
			if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.Returning) == 0)
			{
				if (m_TransformData.HasComponent(target.m_Target))
				{
					destination.m_Value2 = 30f;
				}
			}
			else
			{
				destination.m_Methods |= PathMethod.SpecialParking;
				destination.m_RandomCost = 30f;
			}
			VehicleUtils.SetupPathfind(item: new SetupQueueItem(vehicleEntity, parameters, origin, destination), currentLane: ref currentLane, pathOwner: ref pathOwner, queue: m_PathfindQueue);
		}

		private void CheckServiceDispatches(Entity vehicleEntity, DynamicBuffer<ServiceDispatch> serviceDispatches, MaintenanceVehicleData prefabMaintenanceVehicleData, ref Game.Vehicles.MaintenanceVehicle maintenanceVehicle, ref PathOwner pathOwner)
		{
			if (serviceDispatches.Length <= maintenanceVehicle.m_RequestCount)
			{
				return;
			}
			int num = -1;
			Entity entity = Entity.Null;
			PathElement pathElement = default(PathElement);
			bool flag = false;
			int num2 = 0;
			if (maintenanceVehicle.m_RequestCount >= 1 && (maintenanceVehicle.m_State & MaintenanceVehicleFlags.Returning) == 0)
			{
				DynamicBuffer<PathElement> dynamicBuffer = m_PathElements[vehicleEntity];
				num2 = 1;
				if (pathOwner.m_ElementIndex < dynamicBuffer.Length)
				{
					pathElement = dynamicBuffer[dynamicBuffer.Length - 1];
					flag = true;
				}
			}
			for (int i = num2; i < maintenanceVehicle.m_RequestCount; i++)
			{
				Entity request = serviceDispatches[i].m_Request;
				if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
				{
					pathElement = bufferData[bufferData.Length - 1];
					flag = true;
				}
			}
			for (int j = maintenanceVehicle.m_RequestCount; j < serviceDispatches.Length; j++)
			{
				Entity request2 = serviceDispatches[j].m_Request;
				if (!m_MaintenanceRequestData.HasComponent(request2))
				{
					continue;
				}
				MaintenanceRequest maintenanceRequest = m_MaintenanceRequestData[request2];
				if (flag && m_PathElements.TryGetBuffer(request2, out var bufferData2) && bufferData2.Length != 0)
				{
					PathElement pathElement2 = bufferData2[0];
					if (pathElement2.m_Target != pathElement.m_Target || pathElement2.m_TargetDelta.x != pathElement.m_TargetDelta.y)
					{
						continue;
					}
				}
				if (m_PrefabRefData.HasComponent(maintenanceRequest.m_Target) && maintenanceRequest.m_Priority > num)
				{
					num = maintenanceRequest.m_Priority;
					entity = request2;
				}
			}
			if (entity != Entity.Null)
			{
				serviceDispatches[maintenanceVehicle.m_RequestCount++] = new ServiceDispatch(entity);
				MaintenanceRequest maintenanceRequest2 = m_MaintenanceRequestData[entity];
				if ((prefabMaintenanceVehicleData.m_MaintenanceType & (MaintenanceType.Road | MaintenanceType.Snow)) != MaintenanceType.None && m_NetConditionData.HasComponent(maintenanceRequest2.m_Target))
				{
					maintenanceVehicle.m_MaintainEstimate += PreAddMaintenanceRequests(entity);
				}
				if ((prefabMaintenanceVehicleData.m_MaintenanceType & MaintenanceType.Vehicle) != MaintenanceType.None)
				{
					Damaged componentData2;
					if (m_DestroyedData.TryGetComponent(maintenanceRequest2.m_Target, out var componentData))
					{
						float f = 500f * (1f - componentData.m_Cleared);
						maintenanceVehicle.m_MaintainEstimate += Mathf.RoundToInt(f);
					}
					else if (m_DamagedData.TryGetComponent(maintenanceRequest2.m_Target, out componentData2))
					{
						float f2 = math.min(500f, math.csum(componentData2.m_Damage) * 500f);
						maintenanceVehicle.m_MaintainEstimate += Mathf.RoundToInt(f2);
					}
				}
				if ((prefabMaintenanceVehicleData.m_MaintenanceType & MaintenanceType.Park) != MaintenanceType.None && m_ParkData.TryGetComponent(maintenanceRequest2.m_Target, out var componentData3))
				{
					PrefabRef prefabRef = m_PrefabRefData[maintenanceRequest2.m_Target];
					if (m_PrefabParkData.TryGetComponent(prefabRef.m_Prefab, out var componentData4))
					{
						maintenanceVehicle.m_MaintainEstimate += math.max(0, componentData4.m_MaintenancePool - componentData3.m_Maintenance);
					}
				}
			}
			if (serviceDispatches.Length > maintenanceVehicle.m_RequestCount)
			{
				serviceDispatches.RemoveRange(maintenanceVehicle.m_RequestCount, serviceDispatches.Length - maintenanceVehicle.m_RequestCount);
			}
		}

		private int BumpDispachIndex(Entity request)
		{
			int result = 0;
			if (m_MaintenanceRequestData.TryGetComponent(request, out var componentData))
			{
				result = componentData.m_DispatchIndex + 1;
				m_ActionQueue.Enqueue(new MaintenanceAction
				{
					m_Type = MaintenanceActionType.BumpDispatchIndex,
					m_Request = request
				});
			}
			return result;
		}

		private int PreAddMaintenanceRequests(Entity request)
		{
			int num = 0;
			if (m_PathElements.TryGetBuffer(request, out var bufferData))
			{
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
					Owner owner = m_OwnerData[pathElement.m_Target];
					if (!(owner.m_Owner == entity))
					{
						entity = owner.m_Owner;
						if (HasSidewalk(owner.m_Owner))
						{
							num += AddMaintenanceRequests(owner.m_Owner, request, dispatchIndex, collectMaintenance: true);
						}
					}
				}
			}
			return num;
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

		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Vehicles.MaintenanceVehicle maintenanceVehicle)
		{
			if (!m_MaintenanceRequestData.HasComponent(maintenanceVehicle.m_TargetRequest))
			{
				uint num = math.max(512u, 16u);
				if ((m_SimulationFrameIndex & (num - 1)) == 7)
				{
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_MaintenanceRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e, new MaintenanceRequest(entity, 1));
					m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
				}
			}
		}

		private bool SelectNextDispatch(int jobIndex, Entity vehicleEntity, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, MaintenanceVehicleData prefabMaintenanceVehicleData, ref Game.Vehicles.MaintenanceVehicle maintenanceVehicle, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.Returning) == 0 && maintenanceVehicle.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				serviceDispatches.RemoveAt(0);
				maintenanceVehicle.m_RequestCount--;
			}
			while (maintenanceVehicle.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				Entity entity = Entity.Null;
				if (m_MaintenanceRequestData.HasComponent(request))
				{
					entity = m_MaintenanceRequestData[request].m_Target;
				}
				if (!m_PrefabRefData.HasComponent(entity))
				{
					serviceDispatches.RemoveAt(0);
					maintenanceVehicle.m_MaintainEstimate -= maintenanceVehicle.m_MaintainEstimate / maintenanceVehicle.m_RequestCount;
					maintenanceVehicle.m_RequestCount--;
					continue;
				}
				MaintenanceVehicleFlags maintenanceVehicleFlags = (MaintenanceVehicleFlags)0u;
				if (m_NetConditionData.HasComponent(entity))
				{
					maintenanceVehicleFlags |= MaintenanceVehicleFlags.EdgeTarget;
				}
				else if (m_TransformData.HasComponent(entity))
				{
					maintenanceVehicleFlags |= MaintenanceVehicleFlags.TransformTarget;
				}
				if ((maintenanceVehicle.m_State & maintenanceVehicleFlags & MaintenanceVehicleFlags.EdgeTarget) == 0)
				{
					car.m_Flags &= ~(CarFlags.Warning | CarFlags.Working | CarFlags.SignalAnimation1 | CarFlags.SignalAnimation2);
				}
				maintenanceVehicle.m_State &= ~(MaintenanceVehicleFlags.Returning | MaintenanceVehicleFlags.TransformTarget | MaintenanceVehicleFlags.EdgeTarget | MaintenanceVehicleFlags.TryWork | MaintenanceVehicleFlags.Working | MaintenanceVehicleFlags.ClearingDebris);
				maintenanceVehicle.m_State |= maintenanceVehicleFlags;
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, vehicleEntity, completed: false, pathConsumed: true));
				if (m_MaintenanceRequestData.HasComponent(maintenanceVehicle.m_TargetRequest))
				{
					e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(maintenanceVehicle.m_TargetRequest, Entity.Null, completed: true));
				}
				m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(EffectsUpdated));
				if (m_PathElements.HasBuffer(request))
				{
					DynamicBuffer<PathElement> appendPath = m_PathElements[request];
					if (appendPath.Length != 0)
					{
						DynamicBuffer<PathElement> dynamicBuffer = m_PathElements[vehicleEntity];
						PathUtils.TrimPath(dynamicBuffer, ref pathOwner);
						float num = maintenanceVehicle.m_PathElementTime * (float)dynamicBuffer.Length + m_PathInformationData[request].m_Duration;
						if (PathUtils.TryAppendPath(ref currentLane, navigationLanes, dynamicBuffer, appendPath, m_SlaveLaneData, m_OwnerData, m_SubLanes, out var appendedCount))
						{
							int num2 = 0;
							bool flag = maintenanceVehicle.m_RequestCount == 1;
							if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.EdgeTarget) != 0)
							{
								int dispatchIndex = BumpDispachIndex(request);
								int num3 = dynamicBuffer.Length - appendedCount;
								for (int i = 0; i < num3; i++)
								{
									PathElement pathElement = dynamicBuffer[i];
									if (m_PedestrianLaneData.HasComponent(pathElement.m_Target))
									{
										num2 += AddMaintenanceRequests(m_OwnerData[pathElement.m_Target].m_Owner, request, dispatchIndex, flag);
									}
								}
								if (appendedCount > 0)
								{
									NativeArray<PathElement> nativeArray = new NativeArray<PathElement>(appendedCount, Allocator.Temp);
									for (int j = 0; j < appendedCount; j++)
									{
										nativeArray[j] = dynamicBuffer[num3 + j];
									}
									dynamicBuffer.RemoveRange(num3, appendedCount);
									Entity lastOwner = Entity.Null;
									for (int k = 0; k < nativeArray.Length; k++)
									{
										num2 += AddPathElement(dynamicBuffer, nativeArray[k], request, dispatchIndex, ref lastOwner, flag);
									}
									nativeArray.Dispose();
								}
								car.m_Flags |= CarFlags.StayOnRoad;
							}
							else if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.TransformTarget) != 0)
							{
								if (flag)
								{
									if ((prefabMaintenanceVehicleData.m_MaintenanceType & MaintenanceType.Vehicle) != MaintenanceType.None)
									{
										Damaged componentData2;
										if (m_DestroyedData.TryGetComponent(entity, out var componentData))
										{
											float f = 500f * (1f - componentData.m_Cleared);
											num2 += Mathf.RoundToInt(f);
										}
										else if (m_DamagedData.TryGetComponent(entity, out componentData2))
										{
											float f2 = math.min(500f, math.csum(componentData2.m_Damage) * 500f);
											num2 += Mathf.RoundToInt(f2);
										}
									}
									if ((prefabMaintenanceVehicleData.m_MaintenanceType & MaintenanceType.Park) != MaintenanceType.None && m_ParkData.TryGetComponent(entity, out var componentData3))
									{
										PrefabRef prefabRef = m_PrefabRefData[entity];
										if (m_PrefabParkData.TryGetComponent(prefabRef.m_Prefab, out var componentData4))
										{
											num2 += math.max(0, componentData4.m_MaintenancePool - componentData3.m_Maintenance);
										}
									}
								}
								if (m_VehicleData.HasComponent(entity))
								{
									car.m_Flags |= CarFlags.StayOnRoad;
								}
								else
								{
									car.m_Flags &= ~CarFlags.StayOnRoad;
								}
							}
							else
							{
								car.m_Flags |= CarFlags.StayOnRoad;
							}
							if (flag)
							{
								maintenanceVehicle.m_MaintainEstimate = num2;
							}
							if ((prefabMaintenanceVehicleData.m_MaintenanceType & (MaintenanceType.Road | MaintenanceType.Snow | MaintenanceType.Vehicle)) != MaintenanceType.None)
							{
								car.m_Flags |= CarFlags.UsePublicTransportLanes;
							}
							maintenanceVehicle.m_PathElementTime = num / (float)math.max(1, dynamicBuffer.Length);
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

		private void ReturnToDepot(int jobIndex, Entity vehicleEntity, Owner ownerData, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Car car, ref Game.Vehicles.MaintenanceVehicle maintenanceVehicle, ref PathOwner pathOwnerData, ref Target targetData)
		{
			serviceDispatches.Clear();
			car.m_Flags &= ~(CarFlags.Warning | CarFlags.Working | CarFlags.SignalAnimation1 | CarFlags.SignalAnimation2);
			maintenanceVehicle.m_RequestCount = 0;
			maintenanceVehicle.m_MaintainEstimate = 0;
			maintenanceVehicle.m_State &= ~(MaintenanceVehicleFlags.TransformTarget | MaintenanceVehicleFlags.EdgeTarget | MaintenanceVehicleFlags.TryWork | MaintenanceVehicleFlags.Working | MaintenanceVehicleFlags.ClearingDebris);
			maintenanceVehicle.m_State |= MaintenanceVehicleFlags.Returning;
			VehicleUtils.SetTarget(ref pathOwnerData, ref targetData, ownerData.m_Owner);
			m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(EffectsUpdated));
		}

		private void ResetPath(int jobIndex, Entity vehicleEntity, PathInformation pathInformation, DynamicBuffer<ServiceDispatch> serviceDispatches, MaintenanceVehicleData prefabMaintenanceVehicleData, ref Unity.Mathematics.Random random, ref Game.Vehicles.MaintenanceVehicle maintenanceVehicle, ref Car carData, ref CarCurrentLane currentLane, ref PathOwner pathOwner)
		{
			DynamicBuffer<PathElement> path = m_PathElements[vehicleEntity];
			PathUtils.ResetPath(ref currentLane, path, m_SlaveLaneData, m_OwnerData, m_SubLanes);
			VehicleUtils.ResetParkingLaneStatus(vehicleEntity, ref currentLane, ref pathOwner, path, ref m_EntityLookup, ref m_CurveData, ref m_ParkingLaneData, ref m_CarLaneData, ref m_ConnectionLaneData, ref m_SpawnLocationData, ref m_PrefabRefData, ref m_PrefabSpawnLocationData);
			VehicleUtils.SetParkingCurvePos(vehicleEntity, ref random, currentLane, pathOwner, path, ref m_ParkedCarData, ref m_UnspawnedData, ref m_CurveData, ref m_ParkingLaneData, ref m_ConnectionLaneData, ref m_PrefabRefData, ref m_PrefabObjectGeometryData, ref m_PrefabParkingLaneData, ref m_LaneObjects, ref m_LaneOverlaps, ignoreDriveways: false);
			currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.Checked;
			if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.Returning) == 0 && maintenanceVehicle.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				if (m_MaintenanceRequestData.HasComponent(request))
				{
					Entity target = m_MaintenanceRequestData[request].m_Target;
					int num = 0;
					bool flag = maintenanceVehicle.m_RequestCount == 1;
					if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.EdgeTarget) != 0)
					{
						NativeArray<PathElement> nativeArray = new NativeArray<PathElement>(path.Length, Allocator.Temp);
						nativeArray.CopyFrom(path.AsNativeArray());
						path.Clear();
						Entity lastOwner = Entity.Null;
						int dispatchIndex = BumpDispachIndex(request);
						for (int i = 0; i < nativeArray.Length; i++)
						{
							num += AddPathElement(path, nativeArray[i], request, dispatchIndex, ref lastOwner, flag);
						}
						nativeArray.Dispose();
						carData.m_Flags |= CarFlags.StayOnRoad;
					}
					else if (m_TransformData.HasComponent(target))
					{
						if (flag)
						{
							if ((prefabMaintenanceVehicleData.m_MaintenanceType & MaintenanceType.Vehicle) != MaintenanceType.None)
							{
								Damaged componentData2;
								if (m_DestroyedData.TryGetComponent(target, out var componentData))
								{
									float f = 500f * (1f - componentData.m_Cleared);
									num += Mathf.RoundToInt(f);
								}
								else if (m_DamagedData.TryGetComponent(target, out componentData2))
								{
									float f2 = math.min(500f, math.csum(componentData2.m_Damage) * 500f);
									num += Mathf.RoundToInt(f2);
								}
							}
							if ((prefabMaintenanceVehicleData.m_MaintenanceType & MaintenanceType.Park) != MaintenanceType.None && m_ParkData.TryGetComponent(target, out var componentData3))
							{
								PrefabRef prefabRef = m_PrefabRefData[target];
								if (m_PrefabParkData.TryGetComponent(prefabRef.m_Prefab, out var componentData4))
								{
									num += math.max(0, componentData4.m_MaintenancePool - componentData3.m_Maintenance);
								}
							}
						}
						if (m_VehicleData.HasComponent(target))
						{
							carData.m_Flags |= CarFlags.StayOnRoad;
						}
						else
						{
							carData.m_Flags &= ~CarFlags.StayOnRoad;
						}
					}
					else
					{
						carData.m_Flags |= CarFlags.StayOnRoad;
					}
					if (flag)
					{
						maintenanceVehicle.m_MaintainEstimate = num;
					}
				}
				else
				{
					carData.m_Flags |= CarFlags.StayOnRoad;
				}
			}
			else
			{
				carData.m_Flags &= ~CarFlags.StayOnRoad;
			}
			if ((prefabMaintenanceVehicleData.m_MaintenanceType & (MaintenanceType.Road | MaintenanceType.Snow | MaintenanceType.Vehicle)) != MaintenanceType.None)
			{
				carData.m_Flags |= CarFlags.UsePublicTransportLanes;
			}
			maintenanceVehicle.m_PathElementTime = pathInformation.m_Duration / (float)math.max(1, path.Length);
		}

		private int AddPathElement(DynamicBuffer<PathElement> path, PathElement pathElement, Entity request, int dispatchIndex, ref Entity lastOwner, bool collectMaintenance)
		{
			int num = 0;
			if (!m_EdgeLaneData.HasComponent(pathElement.m_Target))
			{
				path.Add(pathElement);
				lastOwner = Entity.Null;
				return num;
			}
			Owner owner = m_OwnerData[pathElement.m_Target];
			if (owner.m_Owner == lastOwner)
			{
				path.Add(pathElement);
				return num;
			}
			lastOwner = owner.m_Owner;
			float curvePos = pathElement.m_TargetDelta.y;
			if (FindClosestSidewalk(pathElement.m_Target, owner.m_Owner, ref curvePos, out var sidewalk))
			{
				num += AddMaintenanceRequests(owner.m_Owner, request, dispatchIndex, collectMaintenance);
				path.Add(pathElement);
				path.Add(new PathElement(sidewalk, curvePos));
			}
			else
			{
				path.Add(pathElement);
			}
			return num;
		}

		private bool FindClosestSidewalk(Entity lane, Entity owner, ref float curvePos, out Entity sidewalk)
		{
			bool result = false;
			sidewalk = Entity.Null;
			if (m_SubLanes.HasBuffer(owner))
			{
				float3 position = MathUtils.Position(m_CurveData[lane].m_Bezier, curvePos);
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[owner];
				float num = float.MaxValue;
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
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

		private int AddMaintenanceRequests(Entity edgeEntity, Entity request, int dispatchIndex, bool collectMaintenance)
		{
			int result = 0;
			if (m_NetConditionData.HasComponent(edgeEntity))
			{
				if (collectMaintenance)
				{
					Game.Net.Edge edge = m_EdgeData[edgeEntity];
					result = Mathf.RoundToInt(CalculateTotalLaneWear(edgeEntity) + (CalculateTotalLaneWear(edge.m_Start) + CalculateTotalLaneWear(edge.m_End)) * 0.5f);
				}
				m_ActionQueue.Enqueue(new MaintenanceAction
				{
					m_Type = MaintenanceActionType.AddRequest,
					m_Consumer = edgeEntity,
					m_Request = request,
					m_VehicleCapacity = dispatchIndex
				});
			}
			return result;
		}

		private float CalculateTotalLaneWear(Entity owner)
		{
			float num = 0f;
			if (m_SubLanes.HasBuffer(owner))
			{
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[owner];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					if (m_LaneConditionData.HasComponent(subLane))
					{
						LaneCondition laneCondition = m_LaneConditionData[subLane];
						Curve curve = m_CurveData[subLane];
						num += laneCondition.m_Wear * curve.m_Length * 0.01f;
					}
				}
			}
			return num;
		}

		private void CheckMaintenancePresence(ref Car car, ref Game.Vehicles.MaintenanceVehicle maintenanceVehicle, ref CarCurrentLane currentLane, DynamicBuffer<CarNavigationLane> navigationLanes)
		{
			if ((maintenanceVehicle.m_State & MaintenanceVehicleFlags.ClearChecked) != 0)
			{
				if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Waypoint) != 0)
				{
					currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.Checked;
				}
				maintenanceVehicle.m_State &= ~MaintenanceVehicleFlags.ClearChecked;
			}
			if ((currentLane.m_LaneFlags & (Game.Vehicles.CarLaneFlags.Waypoint | Game.Vehicles.CarLaneFlags.Checked)) == Game.Vehicles.CarLaneFlags.Waypoint)
			{
				if (!CheckMaintenancePresence(currentLane.m_Lane))
				{
					currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.Waypoint;
					car.m_Flags &= ~(CarFlags.Warning | CarFlags.Working | CarFlags.SignalAnimation1 | CarFlags.SignalAnimation2);
					if (m_SlaveLaneData.HasComponent(currentLane.m_Lane))
					{
						currentLane.m_LaneFlags &= ~Game.Vehicles.CarLaneFlags.FixedLane;
					}
				}
				currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.Checked;
			}
			if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Waypoint) != 0)
			{
				car.m_Flags |= CarFlags.Warning;
				if ((car.m_Flags & CarFlags.Working) == 0 && math.abs(currentLane.m_CurvePosition.x - currentLane.m_CurvePosition.z) < 0.5f)
				{
					car.m_Flags |= GetWorkingFlags(ref currentLane);
				}
				return;
			}
			for (int i = 0; i < navigationLanes.Length; i++)
			{
				ref CarNavigationLane reference = ref navigationLanes.ElementAt(i);
				if ((reference.m_Flags & Game.Vehicles.CarLaneFlags.Waypoint) != 0 && (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Checked) == 0)
				{
					if (!CheckMaintenancePresence(reference.m_Lane))
					{
						reference.m_Flags &= ~Game.Vehicles.CarLaneFlags.Waypoint;
						car.m_Flags &= ~CarFlags.Warning;
						if (m_SlaveLaneData.HasComponent(reference.m_Lane))
						{
							reference.m_Flags &= ~Game.Vehicles.CarLaneFlags.FixedLane;
						}
					}
					currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.Checked;
					car.m_Flags &= ~(CarFlags.Working | CarFlags.SignalAnimation1 | CarFlags.SignalAnimation2);
				}
				if ((reference.m_Flags & (Game.Vehicles.CarLaneFlags.Reserved | Game.Vehicles.CarLaneFlags.Waypoint)) != Game.Vehicles.CarLaneFlags.Reserved)
				{
					car.m_Flags &= ~(CarFlags.Working | CarFlags.SignalAnimation1 | CarFlags.SignalAnimation2);
					break;
				}
			}
		}

		private CarFlags GetWorkingFlags(ref CarCurrentLane currentLaneData)
		{
			Game.Net.CarLaneFlags carLaneFlags = Game.Net.CarLaneFlags.RightLimit | Game.Net.CarLaneFlags.LeftLimit;
			if (m_CarLaneData.TryGetComponent(currentLaneData.m_Lane, out var componentData))
			{
				carLaneFlags &= componentData.m_Flags;
			}
			switch (carLaneFlags)
			{
			case Game.Net.CarLaneFlags.RightLimit:
				return CarFlags.Working | CarFlags.SignalAnimation1;
			case Game.Net.CarLaneFlags.LeftLimit:
				return CarFlags.Working | CarFlags.SignalAnimation2;
			default:
				if (!m_LeftHandTraffic)
				{
					return CarFlags.Working | CarFlags.SignalAnimation1;
				}
				return CarFlags.Working | CarFlags.SignalAnimation2;
			}
		}

		private bool CheckMaintenancePresence(Entity laneEntity)
		{
			if (m_EdgeLaneData.HasComponent(laneEntity) && m_OwnerData.TryGetComponent(laneEntity, out var componentData) && m_NetConditionData.TryGetComponent(componentData.m_Owner, out var componentData2))
			{
				return math.any(componentData2.m_Wear >= 0.099999994f);
			}
			return false;
		}

		private void TryMaintain(Entity vehicleEntity, MaintenanceVehicleData prefabMaintenanceVehicleData, ref Car car, ref Game.Vehicles.MaintenanceVehicle maintenanceVehicle, ref CarCurrentLane currentLaneData)
		{
			maintenanceVehicle.m_State |= MaintenanceVehicleFlags.TryWork;
			maintenanceVehicle.m_State &= ~MaintenanceVehicleFlags.Working;
			if (maintenanceVehicle.m_Maintained < prefabMaintenanceVehicleData.m_MaintenanceCapacity)
			{
				TryMaintainLane(vehicleEntity, prefabMaintenanceVehicleData, currentLaneData.m_Lane, ref currentLaneData);
			}
		}

		private void TryMaintainLane(Entity vehicleEntity, MaintenanceVehicleData prefabMaintenanceVehicleData, Entity laneEntity, ref CarCurrentLane currentLaneData)
		{
			if (m_EdgeLaneData.HasComponent(laneEntity) && m_OwnerData.TryGetComponent(laneEntity, out var componentData) && m_NetConditionData.TryGetComponent(componentData.m_Owner, out var componentData2) && math.any(componentData2.m_Wear >= 0.099999994f))
			{
				m_ActionQueue.Enqueue(new MaintenanceAction
				{
					m_Type = MaintenanceActionType.RoadMaintenance,
					m_Vehicle = vehicleEntity,
					m_Consumer = componentData.m_Owner,
					m_VehicleCapacity = prefabMaintenanceVehicleData.m_MaintenanceCapacity,
					m_ConsumerCapacity = 0,
					m_MaxMaintenanceAmount = Mathf.RoundToInt((float)(prefabMaintenanceVehicleData.m_MaintenanceRate * 16) / 60f),
					m_WorkingFlags = GetWorkingFlags(ref currentLaneData)
				});
				m_ActionQueue.Enqueue(new MaintenanceAction
				{
					m_Type = MaintenanceActionType.ClearLane,
					m_Vehicle = vehicleEntity,
					m_Consumer = laneEntity
				});
			}
		}

		private void TryMaintain(Entity vehicleEntity, MaintenanceVehicleData prefabMaintenanceVehicleData, ref Car car, ref Game.Vehicles.MaintenanceVehicle maintenanceVehicle, ref CarCurrentLane currentLaneData, ref Target targetData)
		{
			maintenanceVehicle.m_State |= MaintenanceVehicleFlags.TryWork;
			maintenanceVehicle.m_State &= ~MaintenanceVehicleFlags.Working;
			if (maintenanceVehicle.m_Maintained < prefabMaintenanceVehicleData.m_MaintenanceCapacity && m_PrefabRefData.HasComponent(targetData.m_Target))
			{
				PrefabRef prefabRef = m_PrefabRefData[targetData.m_Target];
				if (m_VehicleData.HasComponent(targetData.m_Target))
				{
					m_ActionQueue.Enqueue(new MaintenanceAction
					{
						m_Type = MaintenanceActionType.RepairVehicle,
						m_Vehicle = vehicleEntity,
						m_Consumer = targetData.m_Target,
						m_VehicleCapacity = prefabMaintenanceVehicleData.m_MaintenanceCapacity,
						m_MaxMaintenanceAmount = Mathf.RoundToInt((float)(prefabMaintenanceVehicleData.m_MaintenanceRate * 16) / 60f),
						m_WorkingFlags = GetWorkingFlags(ref currentLaneData)
					});
				}
				else if (m_PrefabParkData.HasComponent(prefabRef.m_Prefab))
				{
					ParkData parkData = m_PrefabParkData[prefabRef.m_Prefab];
					m_ActionQueue.Enqueue(new MaintenanceAction
					{
						m_Type = MaintenanceActionType.ParkMaintenance,
						m_Vehicle = vehicleEntity,
						m_Consumer = targetData.m_Target,
						m_VehicleCapacity = prefabMaintenanceVehicleData.m_MaintenanceCapacity,
						m_ConsumerCapacity = parkData.m_MaintenancePool,
						m_MaxMaintenanceAmount = Mathf.RoundToInt((float)(prefabMaintenanceVehicleData.m_MaintenanceRate * 16) / 60f),
						m_WorkingFlags = CarFlags.Working
					});
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct MaintenanceJob : IJob
	{
		[ReadOnly]
		public EntityArchetype m_DamageEventArchetype;

		[ReadOnly]
		public ComponentLookup<Game.Net.Edge> m_EdgeData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> m_SubLanes;

		[ReadOnly]
		public BufferLookup<LaneObject> m_LaneObjects;

		public ComponentLookup<Car> m_CarData;

		public ComponentLookup<MaintenanceRequest> m_MaintenanceRequestData;

		public ComponentLookup<Game.Vehicles.MaintenanceVehicle> m_MaintenanceVehicleData;

		public ComponentLookup<MaintenanceConsumer> m_MaintenanceConsumerData;

		public ComponentLookup<Damaged> m_DamagedData;

		public ComponentLookup<Destroyed> m_DestroyedData;

		public ComponentLookup<Game.Buildings.Park> m_ParkData;

		public ComponentLookup<NetCondition> m_NetConditionData;

		public ComponentLookup<LaneCondition> m_LaneConditionData;

		public NativeQueue<MaintenanceAction> m_ActionQueue;

		public EntityCommandBuffer m_CommandBuffer;

		public void Execute()
		{
			MaintenanceAction item;
			while (m_ActionQueue.TryDequeue(out item))
			{
				switch (item.m_Type)
				{
				case MaintenanceActionType.AddRequest:
				{
					MaintenanceConsumer value11 = m_MaintenanceConsumerData[item.m_Consumer];
					value11.m_Request = item.m_Request;
					value11.m_DispatchIndex = (byte)item.m_VehicleCapacity;
					m_MaintenanceConsumerData[item.m_Consumer] = value11;
					break;
				}
				case MaintenanceActionType.ParkMaintenance:
				{
					Car value4 = m_CarData[item.m_Vehicle];
					Game.Vehicles.MaintenanceVehicle value5 = m_MaintenanceVehicleData[item.m_Vehicle];
					Game.Buildings.Park value6 = m_ParkData[item.m_Consumer];
					int x2 = math.min(item.m_VehicleCapacity - value5.m_Maintained, item.m_ConsumerCapacity - value6.m_Maintenance);
					x2 = math.min(x2, item.m_MaxMaintenanceAmount);
					if (x2 > 0)
					{
						value5.m_Maintained += x2;
						value5.m_MaintainEstimate = math.max(0, value5.m_MaintainEstimate - x2);
						value6.m_Maintenance += (short)x2;
						value5.m_State |= MaintenanceVehicleFlags.Working;
						value4.m_Flags |= CarFlags.Warning | item.m_WorkingFlags;
						m_CarData[item.m_Vehicle] = value4;
						m_MaintenanceVehicleData[item.m_Vehicle] = value5;
						m_ParkData[item.m_Consumer] = value6;
					}
					break;
				}
				case MaintenanceActionType.RoadMaintenance:
				{
					Car value2 = m_CarData[item.m_Vehicle];
					Game.Vehicles.MaintenanceVehicle value3 = m_MaintenanceVehicleData[item.m_Vehicle];
					Game.Net.Edge edge = m_EdgeData[item.m_Consumer];
					float num = CalculateTotalLaneWear(item.m_Consumer);
					num += CalculateTotalLaneWear(edge.m_Start);
					num += CalculateTotalLaneWear(edge.m_End);
					int x = math.min(item.m_VehicleCapacity - value3.m_Maintained, (int)math.ceil(num));
					x = math.min(x, item.m_MaxMaintenanceAmount);
					if (x > 0)
					{
						value3.m_Maintained += x;
						value3.m_MaintainEstimate = math.max(0, value3.m_MaintainEstimate - x);
						float maintainFactor = math.saturate(1f - (float)x / num);
						value3.m_State |= MaintenanceVehicleFlags.Working;
						value2.m_Flags &= ~(CarFlags.SignalAnimation1 | CarFlags.SignalAnimation2);
						value2.m_Flags |= CarFlags.Warning | item.m_WorkingFlags;
						float2 wear = MaintainLanes(item.m_Consumer, maintainFactor);
						float2 wear2 = MaintainLanes(edge.m_Start, maintainFactor);
						float2 wear3 = MaintainLanes(edge.m_End, maintainFactor);
						if (m_NetConditionData.TryGetComponent(item.m_Consumer, out var componentData2))
						{
							componentData2.m_Wear = wear;
							m_NetConditionData[item.m_Consumer] = componentData2;
						}
						if (m_NetConditionData.TryGetComponent(edge.m_Start, out var componentData3))
						{
							componentData3.m_Wear = wear2;
							m_NetConditionData[edge.m_Start] = componentData3;
						}
						if (m_NetConditionData.TryGetComponent(edge.m_End, out var componentData4))
						{
							componentData4.m_Wear = wear3;
							m_NetConditionData[edge.m_End] = componentData4;
						}
						m_CarData[item.m_Vehicle] = value2;
						m_MaintenanceVehicleData[item.m_Vehicle] = value3;
					}
					break;
				}
				case MaintenanceActionType.RepairVehicle:
				{
					Car value7 = m_CarData[item.m_Vehicle];
					Game.Vehicles.MaintenanceVehicle value8 = m_MaintenanceVehicleData[item.m_Vehicle];
					if (m_DestroyedData.HasComponent(item.m_Consumer))
					{
						Destroyed value9 = m_DestroyedData[item.m_Consumer];
						float num2 = 500f * (1f - value9.m_Cleared);
						int x3 = math.min(item.m_VehicleCapacity - value8.m_Maintained, (int)math.ceil(num2));
						x3 = math.min(x3, item.m_MaxMaintenanceAmount);
						if (x3 > 0)
						{
							value8.m_Maintained += x3;
							value8.m_MaintainEstimate = math.max(0, value8.m_MaintainEstimate - x3);
							value9.m_Cleared = 1f - (1f - value9.m_Cleared) * math.saturate(1f - (float)x3 / num2);
							value8.m_State |= MaintenanceVehicleFlags.Working;
							value7.m_Flags &= ~(CarFlags.SignalAnimation1 | CarFlags.SignalAnimation2);
							value7.m_Flags |= CarFlags.Warning | item.m_WorkingFlags;
							if ((value8.m_State & MaintenanceVehicleFlags.ClearingDebris) == 0)
							{
								value8.m_State |= MaintenanceVehicleFlags.ClearingDebris;
								m_CommandBuffer.AddComponent(item.m_Vehicle, default(EffectsUpdated));
							}
							m_CarData[item.m_Vehicle] = value7;
							m_MaintenanceVehicleData[item.m_Vehicle] = value8;
							m_DestroyedData[item.m_Consumer] = value9;
						}
					}
					else
					{
						if (!m_DamagedData.HasComponent(item.m_Consumer))
						{
							break;
						}
						Damaged value10 = m_DamagedData[item.m_Consumer];
						float num3 = math.min(500f, math.csum(value10.m_Damage) * 500f);
						int x4 = math.min(item.m_VehicleCapacity - value8.m_Maintained, (int)math.ceil(num3));
						x4 = math.min(x4, item.m_MaxMaintenanceAmount);
						if (x4 > 0)
						{
							value8.m_Maintained += x4;
							value8.m_MaintainEstimate = math.max(0, value8.m_MaintainEstimate - x4);
							value10.m_Damage *= math.saturate(1f - (float)x4 / num3);
							value8.m_State |= MaintenanceVehicleFlags.Working;
							value7.m_Flags &= ~(CarFlags.SignalAnimation1 | CarFlags.SignalAnimation2);
							value7.m_Flags |= CarFlags.Warning | item.m_WorkingFlags;
							if ((value8.m_State & MaintenanceVehicleFlags.ClearingDebris) == 0)
							{
								value8.m_State |= MaintenanceVehicleFlags.ClearingDebris;
								m_CommandBuffer.AddComponent(item.m_Vehicle, default(EffectsUpdated));
							}
							m_CarData[item.m_Vehicle] = value7;
							m_MaintenanceVehicleData[item.m_Vehicle] = value8;
							m_DamagedData[item.m_Consumer] = value10;
							if (!math.any(value10.m_Damage > 0f))
							{
								Entity e = m_CommandBuffer.CreateEntity(m_DamageEventArchetype);
								m_CommandBuffer.SetComponent(e, new Damage(item.m_Consumer, new float3(0f, 0f, 0f)));
							}
						}
					}
					break;
				}
				case MaintenanceActionType.ClearLane:
				{
					if (!m_LaneObjects.TryGetBuffer(item.m_Consumer, out var bufferData))
					{
						break;
					}
					for (int i = 0; i < bufferData.Length; i++)
					{
						Entity laneObject = bufferData[i].m_LaneObject;
						if (laneObject != item.m_Vehicle && m_MaintenanceVehicleData.TryGetComponent(laneObject, out var componentData))
						{
							componentData.m_State |= MaintenanceVehicleFlags.ClearChecked;
							m_MaintenanceVehicleData[laneObject] = componentData;
						}
					}
					break;
				}
				case MaintenanceActionType.BumpDispatchIndex:
				{
					MaintenanceRequest value = m_MaintenanceRequestData[item.m_Request];
					value.m_DispatchIndex++;
					m_MaintenanceRequestData[item.m_Request] = value;
					break;
				}
				}
			}
		}

		private float CalculateTotalLaneWear(Entity owner)
		{
			float num = 0f;
			if (m_SubLanes.HasBuffer(owner))
			{
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[owner];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					if (m_LaneConditionData.HasComponent(subLane))
					{
						LaneCondition laneCondition = m_LaneConditionData[subLane];
						Curve curve = m_CurveData[subLane];
						num += laneCondition.m_Wear * curve.m_Length * 0.01f;
					}
				}
			}
			return num;
		}

		private float2 MaintainLanes(Entity owner, float maintainFactor)
		{
			float2 @float = 0f;
			if (m_SubLanes.HasBuffer(owner))
			{
				DynamicBuffer<Game.Net.SubLane> dynamicBuffer = m_SubLanes[owner];
				for (int i = 0; i < dynamicBuffer.Length; i++)
				{
					Entity subLane = dynamicBuffer[i].m_SubLane;
					if (m_LaneConditionData.HasComponent(subLane))
					{
						LaneCondition value = m_LaneConditionData[subLane];
						value.m_Wear *= maintainFactor;
						@float = ((!m_EdgeLaneData.TryGetComponent(subLane, out var componentData)) ? math.max(@float, value.m_Wear) : math.select(@float, value.m_Wear, new bool2(math.any(componentData.m_EdgeDelta == 0f), math.any(componentData.m_EdgeDelta == 1f)) & (value.m_Wear > @float)));
						m_LaneConditionData[subLane] = value;
					}
				}
			}
			return @float;
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
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Stopped> __Game_Objects_Stopped_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Vehicles.MaintenanceVehicle> __Game_Vehicles_MaintenanceVehicle_RW_ComponentTypeHandle;

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
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Unspawned> __Game_Objects_Unspawned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Vehicle> __Game_Vehicles_Vehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Damaged> __Game_Objects_Damaged_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<NetCondition> __Game_Net_NetCondition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<LaneCondition> __Game_Net_LaneCondition_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.CarLane> __Game_Net_CarLane_RO_ComponentLookup;

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
		public ComponentLookup<Game.Net.Edge> __Game_Net_Edge_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<InvolvedInAccident> __Game_Events_InvolvedInAccident_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AccidentSite> __Game_Events_AccidentSite_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<OnFire> __Game_Events_OnFire_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Park> __Game_Buildings_Park_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.MaintenanceDepot> __Game_Buildings_MaintenanceDepot_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CarData> __Game_Prefabs_CarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MaintenanceVehicleData> __Game_Prefabs_MaintenanceVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkData> __Game_Prefabs_ParkData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MaintenanceRequest> __Game_Simulation_MaintenanceRequest_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<TargetElement> __Game_Events_TargetElement_RO_BufferLookup;

		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RW_BufferLookup;

		public ComponentLookup<Car> __Game_Vehicles_Car_RW_ComponentLookup;

		public ComponentLookup<MaintenanceRequest> __Game_Simulation_MaintenanceRequest_RW_ComponentLookup;

		public ComponentLookup<Game.Vehicles.MaintenanceVehicle> __Game_Vehicles_MaintenanceVehicle_RW_ComponentLookup;

		public ComponentLookup<MaintenanceConsumer> __Game_Simulation_MaintenanceConsumer_RW_ComponentLookup;

		public ComponentLookup<Damaged> __Game_Objects_Damaged_RW_ComponentLookup;

		public ComponentLookup<Destroyed> __Game_Common_Destroyed_RW_ComponentLookup;

		public ComponentLookup<Game.Buildings.Park> __Game_Buildings_Park_RW_ComponentLookup;

		public ComponentLookup<NetCondition> __Game_Net_NetCondition_RW_ComponentLookup;

		public ComponentLookup<LaneCondition> __Game_Net_LaneCondition_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Objects_Stopped_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stopped>(isReadOnly: true);
			__Game_Vehicles_MaintenanceVehicle_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.MaintenanceVehicle>();
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
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentLookup = state.GetComponentLookup<Unspawned>(isReadOnly: true);
			__Game_Vehicles_Vehicle_RO_ComponentLookup = state.GetComponentLookup<Vehicle>(isReadOnly: true);
			__Game_Common_Destroyed_RO_ComponentLookup = state.GetComponentLookup<Destroyed>(isReadOnly: true);
			__Game_Objects_Damaged_RO_ComponentLookup = state.GetComponentLookup<Damaged>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Net_NetCondition_RO_ComponentLookup = state.GetComponentLookup<NetCondition>(isReadOnly: true);
			__Game_Net_LaneCondition_RO_ComponentLookup = state.GetComponentLookup<LaneCondition>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_PedestrianLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.PedestrianLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_GarageLane_RO_ComponentLookup = state.GetComponentLookup<GarageLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_Edge_RO_ComponentLookup = state.GetComponentLookup<Game.Net.Edge>(isReadOnly: true);
			__Game_Events_InvolvedInAccident_RO_ComponentLookup = state.GetComponentLookup<InvolvedInAccident>(isReadOnly: true);
			__Game_Events_AccidentSite_RO_ComponentLookup = state.GetComponentLookup<AccidentSite>(isReadOnly: true);
			__Game_Events_OnFire_RO_ComponentLookup = state.GetComponentLookup<OnFire>(isReadOnly: true);
			__Game_Buildings_Park_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Park>(isReadOnly: true);
			__Game_Buildings_MaintenanceDepot_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.MaintenanceDepot>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_MaintenanceVehicleData_RO_ComponentLookup = state.GetComponentLookup<MaintenanceVehicleData>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Prefabs_ParkData_RO_ComponentLookup = state.GetComponentLookup<ParkData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Simulation_MaintenanceRequest_RO_ComponentLookup = state.GetComponentLookup<MaintenanceRequest>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
			__Game_Events_TargetElement_RO_BufferLookup = state.GetBufferLookup<TargetElement>(isReadOnly: true);
			__Game_Pathfind_PathElement_RW_BufferLookup = state.GetBufferLookup<PathElement>();
			__Game_Vehicles_Car_RW_ComponentLookup = state.GetComponentLookup<Car>();
			__Game_Simulation_MaintenanceRequest_RW_ComponentLookup = state.GetComponentLookup<MaintenanceRequest>();
			__Game_Vehicles_MaintenanceVehicle_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.MaintenanceVehicle>();
			__Game_Simulation_MaintenanceConsumer_RW_ComponentLookup = state.GetComponentLookup<MaintenanceConsumer>();
			__Game_Objects_Damaged_RW_ComponentLookup = state.GetComponentLookup<Damaged>();
			__Game_Common_Destroyed_RW_ComponentLookup = state.GetComponentLookup<Destroyed>();
			__Game_Buildings_Park_RW_ComponentLookup = state.GetComponentLookup<Game.Buildings.Park>();
			__Game_Net_NetCondition_RW_ComponentLookup = state.GetComponentLookup<NetCondition>();
			__Game_Net_LaneCondition_RW_ComponentLookup = state.GetComponentLookup<LaneCondition>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private SimulationSystem m_SimulationSystem;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private EntityQuery m_VehicleQuery;

	private EntityArchetype m_DamageEventArchetype;

	private EntityArchetype m_MaintenanceRequestArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private ComponentTypeSet m_MovingToParkedCarRemoveTypes;

	private ComponentTypeSet m_MovingToParkedAddTypes;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 16;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 7;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadWrite<CarCurrentLane>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadWrite<Game.Vehicles.MaintenanceVehicle>(), ComponentType.ReadWrite<Target>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>(), ComponentType.Exclude<OutOfControl>());
		m_DamageEventArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<Damage>());
		m_MaintenanceRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<MaintenanceRequest>(), ComponentType.ReadWrite<RequestGroup>());
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
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<MaintenanceAction> actionQueue = new NativeQueue<MaintenanceAction>(Allocator.TempJob);
		MaintenanceVehicleTickJob jobData = new MaintenanceVehicleTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StoppedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Stopped_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MaintenanceVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_MaintenanceVehicle_RW_ComponentTypeHandle, ref base.CheckedStateRef),
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
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnspawnedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_VehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Vehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RO_ComponentLookup, ref base.CheckedStateRef),
			m_DamagedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_NetConditionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NetCondition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LaneConditionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneCondition_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PedestrianLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_PedestrianLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarageLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_GarageLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InvolvedInAccidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InvolvedInAccident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AccidentSiteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_AccidentSite_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OnFireData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_OnFire_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Park_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MaintenanceDepotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_MaintenanceDepot_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabMaintenanceVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MaintenanceVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabParkData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_MaintenanceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_MaintenanceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneOverlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup, ref base.CheckedStateRef),
			m_TargetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Events_TargetElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_LeftHandTraffic = m_CityConfigurationSystem.leftHandTraffic,
			m_SimulationFrameIndex = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_MaintenanceRequestArchetype = m_MaintenanceRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_MovingToParkedCarRemoveTypes = m_MovingToParkedCarRemoveTypes,
			m_MovingToParkedAddTypes = m_MovingToParkedAddTypes,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
			m_ActionQueue = actionQueue.AsParallelWriter()
		};
		MaintenanceJob jobData2 = new MaintenanceJob
		{
			m_DamageEventArchetype = m_DamageEventArchetype,
			m_EdgeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Edge_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_CarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Car_RW_ComponentLookup, ref base.CheckedStateRef),
			m_MaintenanceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_MaintenanceRequest_RW_ComponentLookup, ref base.CheckedStateRef),
			m_MaintenanceVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_MaintenanceVehicle_RW_ComponentLookup, ref base.CheckedStateRef),
			m_MaintenanceConsumerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_MaintenanceConsumer_RW_ComponentLookup, ref base.CheckedStateRef),
			m_DamagedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Damaged_RW_ComponentLookup, ref base.CheckedStateRef),
			m_DestroyedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Destroyed_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ParkData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Park_RW_ComponentLookup, ref base.CheckedStateRef),
			m_NetConditionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_NetCondition_RW_ComponentLookup, ref base.CheckedStateRef),
			m_LaneConditionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_LaneCondition_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ActionQueue = actionQueue,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer()
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_VehicleQuery, base.Dependency);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		actionQueue.Dispose(jobHandle2);
		m_PathfindSetupSystem.AddQueueWriter(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle2);
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
	public MaintenanceVehicleAISystem()
	{
	}
}
