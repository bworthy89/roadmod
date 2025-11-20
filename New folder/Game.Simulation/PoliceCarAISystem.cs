using System.Runtime.CompilerServices;
using Game.Buildings;
using Game.Common;
using Game.Creatures;
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
public class PoliceCarAISystem : GameSystemBase
{
	private struct PoliceAction
	{
		public PoliceActionType m_Type;

		public Entity m_Target;

		public Entity m_Request;

		public float m_CrimeReductionRate;

		public int m_DispatchIndex;
	}

	private enum PoliceActionType
	{
		ReduceCrime,
		AddPatrolRequest,
		SecureAccidentSite,
		BumpDispatchIndex
	}

	[BurstCompile]
	private struct PoliceCarTickJob : IJobChunk
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

		[ReadOnly]
		public BufferTypeHandle<Passenger> m_PassengerType;

		public ComponentTypeHandle<Game.Vehicles.PoliceCar> m_PoliceCarType;

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
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Unspawned> m_UnspawnedData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<EdgeLane> m_EdgeLaneData;

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
		public ComponentLookup<PoliceCarData> m_PrefabPoliceCarData;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> m_PrefabParkingLaneData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> m_ServiceRequestData;

		[ReadOnly]
		public ComponentLookup<PolicePatrolRequest> m_PolicePatrolRequestData;

		[ReadOnly]
		public ComponentLookup<PoliceEmergencyRequest> m_PoliceEmergencyRequestData;

		[ReadOnly]
		public ComponentLookup<CrimeProducer> m_CrimeProducerData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.PoliceStation> m_PoliceStationData;

		[ReadOnly]
		public ComponentLookup<AccidentSite> m_AccidentSiteData;

		[ReadOnly]
		public ComponentLookup<InvolvedInAccident> m_InvolvedInAccidentData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public BufferLookup<ConnectedBuilding> m_ConnectedBuildings;

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
		public uint m_SimulationFrameIndex;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_PolicePatrolRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_PoliceEmergencyRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedCarRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedAddTypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public NativeQueue<PoliceAction>.ParallelWriter m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<Game.Objects.Transform> nativeArray3 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<PrefabRef> nativeArray4 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<PathInformation> nativeArray5 = chunk.GetNativeArray(ref m_PathInformationType);
			NativeArray<CarCurrentLane> nativeArray6 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<Game.Vehicles.PoliceCar> nativeArray7 = chunk.GetNativeArray(ref m_PoliceCarType);
			NativeArray<Car> nativeArray8 = chunk.GetNativeArray(ref m_CarType);
			NativeArray<Target> nativeArray9 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<PathOwner> nativeArray10 = chunk.GetNativeArray(ref m_PathOwnerType);
			BufferAccessor<Passenger> bufferAccessor = chunk.GetBufferAccessor(ref m_PassengerType);
			BufferAccessor<CarNavigationLane> bufferAccessor2 = chunk.GetBufferAccessor(ref m_CarNavigationLaneType);
			BufferAccessor<ServiceDispatch> bufferAccessor3 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
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
				Game.Vehicles.PoliceCar policeCar = nativeArray7[i];
				Car car = nativeArray8[i];
				CarCurrentLane currentLane = nativeArray6[i];
				PathOwner pathOwner = nativeArray10[i];
				Target target = nativeArray9[i];
				DynamicBuffer<CarNavigationLane> navigationLanes = bufferAccessor2[i];
				DynamicBuffer<ServiceDispatch> serviceDispatches = bufferAccessor3[i];
				DynamicBuffer<Passenger> passengers = default(DynamicBuffer<Passenger>);
				if (bufferAccessor.Length != 0)
				{
					passengers = bufferAccessor[i];
				}
				VehicleUtils.CheckUnspawned(unfilteredChunkIndex, entity, currentLane, isUnspawned, m_CommandBuffer);
				Tick(unfilteredChunkIndex, entity, owner, transform, prefabRef, pathInformation, passengers, navigationLanes, serviceDispatches, isStopped, ref random, ref policeCar, ref car, ref currentLane, ref pathOwner, ref target);
				nativeArray7[i] = policeCar;
				nativeArray8[i] = car;
				nativeArray6[i] = currentLane;
				nativeArray10[i] = pathOwner;
				nativeArray9[i] = target;
			}
		}

		private void Tick(int jobIndex, Entity vehicleEntity, Owner owner, Game.Objects.Transform transform, PrefabRef prefabRef, PathInformation pathInformation, DynamicBuffer<Passenger> passengers, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, bool isStopped, ref Unity.Mathematics.Random random, ref Game.Vehicles.PoliceCar policeCar, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			PoliceCarData prefabPoliceCarData = m_PrefabPoliceCarData[prefabRef.m_Prefab];
			policeCar.m_EstimatedShift = math.select(policeCar.m_EstimatedShift - 1, 0u, policeCar.m_EstimatedShift == 0);
			if (++policeCar.m_ShiftTime >= prefabPoliceCarData.m_ShiftDuration)
			{
				policeCar.m_State |= PoliceCarFlags.ShiftEnded;
			}
			if ((car.m_Flags & CarFlags.Emergency) == 0)
			{
				TryReduceCrime(vehicleEntity, prefabPoliceCarData, ref currentLane, navigationLanes);
			}
			if (VehicleUtils.ResetUpdatedPath(ref pathOwner))
			{
				ResetPath(jobIndex, vehicleEntity, ref random, pathInformation, serviceDispatches, ref policeCar, ref car, ref currentLane, ref pathOwner);
			}
			if (!m_EntityLookup.Exists(target.m_Target) || VehicleUtils.PathfindFailed(pathOwner))
			{
				if (VehicleUtils.IsStuck(pathOwner) || (policeCar.m_State & PoliceCarFlags.Returning) != 0)
				{
					m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					return;
				}
				ReturnToStation(jobIndex, vehicleEntity, owner, serviceDispatches, ref policeCar, ref car, ref pathOwner, ref target);
			}
			else if (VehicleUtils.PathEndReached(currentLane) || VehicleUtils.ParkingSpaceReached(currentLane, pathOwner) || (policeCar.m_State & (PoliceCarFlags.AtTarget | PoliceCarFlags.Disembarking)) != 0)
			{
				if ((policeCar.m_State & PoliceCarFlags.Returning) != 0)
				{
					if ((policeCar.m_State & PoliceCarFlags.Disembarking) != 0)
					{
						if (StopDisembarking(passengers, ref policeCar))
						{
							if (VehicleUtils.ParkingSpaceReached(currentLane, pathOwner))
							{
								ParkCar(jobIndex, vehicleEntity, owner, ref policeCar, ref car, ref currentLane);
							}
							else
							{
								m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
							}
						}
					}
					else if (!StartDisembarking(passengers, ref policeCar))
					{
						if (VehicleUtils.ParkingSpaceReached(currentLane, pathOwner))
						{
							ParkCar(jobIndex, vehicleEntity, owner, ref policeCar, ref car, ref currentLane);
						}
						else
						{
							m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
						}
					}
					return;
				}
				bool flag = true;
				if ((policeCar.m_State & PoliceCarFlags.AccidentTarget) != 0)
				{
					flag &= SecureAccidentSite(jobIndex, vehicleEntity, isStopped, ref policeCar, ref currentLane, passengers, serviceDispatches);
				}
				else
				{
					TryReduceCrime(vehicleEntity, prefabPoliceCarData, ref target);
				}
				if (flag)
				{
					CheckServiceDispatches(vehicleEntity, serviceDispatches, passengers, ref policeCar, ref pathOwner);
					if ((policeCar.m_State & (PoliceCarFlags.ShiftEnded | PoliceCarFlags.Disabled)) != 0 || !SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, passengers, ref policeCar, ref car, ref currentLane, ref pathOwner, ref target))
					{
						ReturnToStation(jobIndex, vehicleEntity, owner, serviceDispatches, ref policeCar, ref car, ref pathOwner, ref target);
					}
				}
			}
			else if (isStopped)
			{
				StartVehicle(jobIndex, vehicleEntity, ref currentLane);
			}
			else if ((policeCar.m_State & PoliceCarFlags.AccidentTarget) != 0 && (currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.IsBlocked) != 0 && IsCloseEnough(transform, ref target))
			{
				EndNavigation(vehicleEntity, ref currentLane, ref pathOwner, navigationLanes);
			}
			if (policeCar.m_ShiftTime + policeCar.m_EstimatedShift >= prefabPoliceCarData.m_ShiftDuration)
			{
				policeCar.m_State |= PoliceCarFlags.EstimatedShiftEnd;
			}
			else
			{
				policeCar.m_State &= ~PoliceCarFlags.EstimatedShiftEnd;
			}
			if (passengers.Length >= prefabPoliceCarData.m_CriminalCapacity)
			{
				policeCar.m_State |= PoliceCarFlags.Full;
			}
			else
			{
				policeCar.m_State &= ~PoliceCarFlags.Full;
			}
			if (passengers.Length == 0)
			{
				policeCar.m_State |= PoliceCarFlags.Empty;
			}
			else
			{
				policeCar.m_State &= ~PoliceCarFlags.Empty;
			}
			if ((car.m_Flags & CarFlags.Emergency) == 0 && (policeCar.m_State & (PoliceCarFlags.ShiftEnded | PoliceCarFlags.Disabled)) != 0)
			{
				if ((policeCar.m_State & PoliceCarFlags.Returning) == 0)
				{
					ReturnToStation(jobIndex, vehicleEntity, owner, serviceDispatches, ref policeCar, ref car, ref pathOwner, ref target);
				}
				serviceDispatches.Clear();
			}
			else
			{
				CheckServiceDispatches(vehicleEntity, serviceDispatches, passengers, ref policeCar, ref pathOwner);
				if ((policeCar.m_State & (PoliceCarFlags.Returning | PoliceCarFlags.Cancelled)) != 0)
				{
					SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, passengers, ref policeCar, ref car, ref currentLane, ref pathOwner, ref target);
				}
				if (policeCar.m_RequestCount <= 1 && (policeCar.m_State & (PoliceCarFlags.ShiftEnded | PoliceCarFlags.Disabled)) == 0)
				{
					RequestTargetIfNeeded(jobIndex, vehicleEntity, ref policeCar);
				}
			}
			if ((policeCar.m_State & (PoliceCarFlags.AtTarget | PoliceCarFlags.Disembarking)) != 0)
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
					FindNewPath(vehicleEntity, prefabRef, ref policeCar, ref car, ref currentLane, ref pathOwner, ref target);
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

		private void ParkCar(int jobIndex, Entity entity, Owner owner, ref Game.Vehicles.PoliceCar policeCar, ref Car car, ref CarCurrentLane currentLane)
		{
			car.m_Flags &= ~CarFlags.Emergency;
			policeCar.m_State = PoliceCarFlags.Empty;
			if (m_PoliceStationData.TryGetComponent(owner.m_Owner, out var componentData) && (componentData.m_Flags & PoliceStationFlags.HasAvailablePatrolCars) == 0)
			{
				policeCar.m_State |= PoliceCarFlags.Disabled;
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

		private bool StartDisembarking(DynamicBuffer<Passenger> passengers, ref Game.Vehicles.PoliceCar policeCar)
		{
			if (passengers.IsCreated && passengers.Length > 0)
			{
				policeCar.m_State |= PoliceCarFlags.Disembarking;
				return true;
			}
			return false;
		}

		private bool StopDisembarking(DynamicBuffer<Passenger> passengers, ref Game.Vehicles.PoliceCar policeCar)
		{
			if (passengers.IsCreated && passengers.Length > 0)
			{
				return false;
			}
			policeCar.m_State &= ~PoliceCarFlags.Disembarking;
			return true;
		}

		private void FindNewPath(Entity vehicleEntity, PrefabRef prefabRef, ref Game.Vehicles.PoliceCar policeCar, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			CarData carData = m_PrefabCarData[prefabRef.m_Prefab];
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = carData.m_MaxSpeed,
				m_WalkSpeed = 5.555556f,
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
			if ((policeCar.m_State & PoliceCarFlags.AccidentTarget) != 0)
			{
				destination.m_Type = SetupTargetType.AccidentLocation;
				destination.m_Value2 = 30f;
			}
			if ((policeCar.m_State & PoliceCarFlags.Returning) == 0)
			{
				parameters.m_Weights = new PathfindWeights(1f, 0f, 0f, 0f);
			}
			else
			{
				parameters.m_Weights = new PathfindWeights(1f, 1f, 1f, 1f);
				destination.m_Methods |= PathMethod.SpecialParking;
				destination.m_RandomCost = 30f;
			}
			VehicleUtils.SetupPathfind(item: new SetupQueueItem(vehicleEntity, parameters, origin, destination), currentLane: ref currentLane, pathOwner: ref pathOwner, queue: m_PathfindQueue);
		}

		private bool SecureAccidentSite(int jobIndex, Entity entity, bool isStopped, ref Game.Vehicles.PoliceCar policeCar, ref CarCurrentLane currentLaneData, DynamicBuffer<Passenger> passengers, DynamicBuffer<ServiceDispatch> serviceDispatches)
		{
			if (policeCar.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				if (m_PoliceEmergencyRequestData.HasComponent(request))
				{
					PoliceEmergencyRequest policeEmergencyRequest = m_PoliceEmergencyRequestData[request];
					if (m_AccidentSiteData.HasComponent(policeEmergencyRequest.m_Site))
					{
						policeCar.m_State |= PoliceCarFlags.AtTarget;
						if (!isStopped)
						{
							StopVehicle(jobIndex, entity, ref currentLaneData);
						}
						if ((m_AccidentSiteData[policeEmergencyRequest.m_Site].m_Flags & AccidentSiteFlags.Secured) == 0)
						{
							m_ActionQueue.Enqueue(new PoliceAction
							{
								m_Type = PoliceActionType.SecureAccidentSite,
								m_Target = policeEmergencyRequest.m_Site
							});
						}
						return false;
					}
				}
			}
			if (passengers.IsCreated)
			{
				for (int i = 0; i < passengers.Length; i++)
				{
					Entity passenger = passengers[i].m_Passenger;
					if (m_CurrentVehicleData.HasComponent(passenger) && (m_CurrentVehicleData[passenger].m_Flags & CreatureVehicleFlags.Ready) == 0)
					{
						return false;
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
			if (m_AccidentSiteData.HasComponent(target.m_Target))
			{
				AccidentSite accidentSite = m_AccidentSiteData[target.m_Target];
				if (m_TargetElements.HasBuffer(accidentSite.m_Event))
				{
					DynamicBuffer<TargetElement> dynamicBuffer = m_TargetElements[accidentSite.m_Event];
					for (int i = 0; i < dynamicBuffer.Length; i++)
					{
						Entity entity = dynamicBuffer[i].m_Entity;
						if (m_InvolvedInAccidentData.HasComponent(entity) && m_TransformData.HasComponent(entity))
						{
							InvolvedInAccident involvedInAccident = m_InvolvedInAccidentData[entity];
							Game.Objects.Transform transform3 = m_TransformData[entity];
							if (involvedInAccident.m_Event == accidentSite.m_Event && math.distance(transform.m_Position, transform3.m_Position) <= 30f)
							{
								return true;
							}
						}
					}
				}
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

		private void CheckServiceDispatches(Entity vehicleEntity, DynamicBuffer<ServiceDispatch> serviceDispatches, DynamicBuffer<Passenger> passengers, ref Game.Vehicles.PoliceCar policeCar, ref PathOwner pathOwner)
		{
			if (serviceDispatches.Length <= policeCar.m_RequestCount)
			{
				return;
			}
			float num = -1f;
			Entity entity = Entity.Null;
			PathElement pathElement = default(PathElement);
			PathElement pathElement2 = default(PathElement);
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			int num2 = 0;
			if (policeCar.m_RequestCount >= 1 && (policeCar.m_State & PoliceCarFlags.Returning) == 0)
			{
				DynamicBuffer<PathElement> dynamicBuffer = m_PathElements[vehicleEntity];
				num2 = 1;
				if (pathOwner.m_ElementIndex < dynamicBuffer.Length)
				{
					pathElement = dynamicBuffer[dynamicBuffer.Length - 1];
					flag = true;
					if (m_PoliceEmergencyRequestData.HasComponent(serviceDispatches[0].m_Request))
					{
						pathElement2 = pathElement;
						flag2 = true;
					}
				}
			}
			for (int i = num2; i < policeCar.m_RequestCount; i++)
			{
				Entity request = serviceDispatches[i].m_Request;
				if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
				{
					pathElement = bufferData[bufferData.Length - 1];
					flag = true;
					if (m_PoliceEmergencyRequestData.HasComponent(request))
					{
						pathElement2 = pathElement;
						flag2 = true;
					}
				}
			}
			for (int j = policeCar.m_RequestCount; j < serviceDispatches.Length; j++)
			{
				Entity request2 = serviceDispatches[j].m_Request;
				if (m_PolicePatrolRequestData.HasComponent(request2))
				{
					if (passengers.IsCreated && passengers.Length != 0)
					{
						continue;
					}
					PolicePatrolRequest policePatrolRequest = m_PolicePatrolRequestData[request2];
					if (flag && m_PathElements.TryGetBuffer(request2, out var bufferData2) && bufferData2.Length != 0)
					{
						PathElement pathElement3 = bufferData2[0];
						if (pathElement3.m_Target != pathElement.m_Target || pathElement3.m_TargetDelta.x != pathElement.m_TargetDelta.y)
						{
							continue;
						}
					}
					if (m_PrefabRefData.HasComponent(policePatrolRequest.m_Target) && !flag3 && policePatrolRequest.m_Priority > num)
					{
						num = policePatrolRequest.m_Priority;
						entity = request2;
					}
				}
				else
				{
					if (!m_PoliceEmergencyRequestData.HasComponent(request2))
					{
						continue;
					}
					PoliceEmergencyRequest policeEmergencyRequest = m_PoliceEmergencyRequestData[request2];
					if (flag2 && m_PathElements.TryGetBuffer(request2, out var bufferData3) && bufferData3.Length != 0)
					{
						PathElement pathElement4 = bufferData3[0];
						if (pathElement4.m_Target != pathElement2.m_Target || pathElement4.m_TargetDelta.x != pathElement2.m_TargetDelta.y)
						{
							continue;
						}
					}
					if (m_PrefabRefData.HasComponent(policeEmergencyRequest.m_Site) && (!flag3 || policeEmergencyRequest.m_Priority > num))
					{
						num = policeEmergencyRequest.m_Priority;
						entity = request2;
						flag3 = true;
					}
				}
			}
			if (flag3)
			{
				int num3 = 0;
				for (int k = 0; k < policeCar.m_RequestCount; k++)
				{
					ServiceDispatch value = serviceDispatches[k];
					if (m_PoliceEmergencyRequestData.HasComponent(value.m_Request))
					{
						serviceDispatches[num3++] = value;
					}
					else if (k == 0 && (policeCar.m_State & PoliceCarFlags.Returning) == 0)
					{
						serviceDispatches[num3++] = value;
						policeCar.m_State |= PoliceCarFlags.Cancelled;
						if (m_PathInformationData.TryGetComponent(value.m_Request, out var componentData))
						{
							uint num4 = (uint)Mathf.RoundToInt(componentData.m_Duration * 3.75f);
							policeCar.m_EstimatedShift = math.select(policeCar.m_EstimatedShift - num4, 0u, num4 >= policeCar.m_EstimatedShift);
						}
					}
				}
				if (num3 < policeCar.m_RequestCount)
				{
					serviceDispatches.RemoveRange(num3, policeCar.m_RequestCount - num3);
					policeCar.m_RequestCount = num3;
				}
			}
			if (entity != Entity.Null)
			{
				serviceDispatches[policeCar.m_RequestCount++] = new ServiceDispatch(entity);
				if (!flag3)
				{
					PreAddPatrolRequests(entity);
				}
				if (m_PathInformationData.TryGetComponent(entity, out var componentData2))
				{
					policeCar.m_EstimatedShift += (uint)Mathf.RoundToInt(componentData2.m_Duration * 3.75f);
				}
			}
			if (serviceDispatches.Length > policeCar.m_RequestCount)
			{
				serviceDispatches.RemoveRange(policeCar.m_RequestCount, serviceDispatches.Length - policeCar.m_RequestCount);
			}
		}

		private int BumpDispachIndex(Entity request)
		{
			int result = 0;
			if (m_PolicePatrolRequestData.TryGetComponent(request, out var componentData))
			{
				result = componentData.m_DispatchIndex + 1;
				m_ActionQueue.Enqueue(new PoliceAction
				{
					m_Type = PoliceActionType.BumpDispatchIndex,
					m_Request = request
				});
			}
			return result;
		}

		private void PreAddPatrolRequests(Entity request)
		{
			if (!m_PathElements.TryGetBuffer(request, out var bufferData))
			{
				return;
			}
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
					AddPatrolRequests(owner.m_Owner, request, dispatchIndex);
				}
			}
		}

		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Vehicles.PoliceCar policeCar)
		{
			if (m_ServiceRequestData.HasComponent(policeCar.m_TargetRequest))
			{
				return;
			}
			if ((policeCar.m_PurposeMask & PolicePurpose.Patrol) != 0 && (policeCar.m_State & (PoliceCarFlags.Empty | PoliceCarFlags.EstimatedShiftEnd)) == PoliceCarFlags.Empty)
			{
				uint num = math.max(512u, 16u);
				if ((m_SimulationFrameIndex & (num - 1)) == 5)
				{
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_PolicePatrolRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e, new PolicePatrolRequest(entity, 1f));
					m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
				}
			}
			else if ((policeCar.m_PurposeMask & (PolicePurpose.Emergency | PolicePurpose.Intelligence)) != 0)
			{
				uint num2 = math.max(64u, 16u);
				if ((m_SimulationFrameIndex & (num2 - 1)) == 5)
				{
					Entity e2 = m_CommandBuffer.CreateEntity(jobIndex, m_PoliceEmergencyRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e2, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e2, new PoliceEmergencyRequest(entity, Entity.Null, 1f, policeCar.m_PurposeMask & (PolicePurpose.Emergency | PolicePurpose.Intelligence)));
					m_CommandBuffer.SetComponent(jobIndex, e2, new RequestGroup(4u));
				}
			}
		}

		private bool SelectNextDispatch(int jobIndex, Entity vehicleEntity, DynamicBuffer<CarNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, DynamicBuffer<Passenger> passengers, ref Game.Vehicles.PoliceCar policeCar, ref Car car, ref CarCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			if ((policeCar.m_State & PoliceCarFlags.Returning) == 0 && policeCar.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				serviceDispatches.RemoveAt(0);
				policeCar.m_RequestCount--;
			}
			while (policeCar.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				Entity entity = Entity.Null;
				PoliceCarFlags policeCarFlags = (PoliceCarFlags)0u;
				if (m_PolicePatrolRequestData.HasComponent(request))
				{
					if (!passengers.IsCreated || passengers.Length == 0)
					{
						entity = m_PolicePatrolRequestData[request].m_Target;
					}
				}
				else if (m_PoliceEmergencyRequestData.HasComponent(request))
				{
					entity = m_PoliceEmergencyRequestData[request].m_Site;
					policeCarFlags |= PoliceCarFlags.AccidentTarget;
				}
				if (!m_PrefabRefData.HasComponent(entity))
				{
					serviceDispatches.RemoveAt(0);
					policeCar.m_EstimatedShift -= policeCar.m_EstimatedShift / (uint)policeCar.m_RequestCount;
					policeCar.m_RequestCount--;
					continue;
				}
				policeCar.m_State &= ~(PoliceCarFlags.Returning | PoliceCarFlags.AccidentTarget | PoliceCarFlags.AtTarget | PoliceCarFlags.Cancelled);
				policeCar.m_State |= policeCarFlags;
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, vehicleEntity, completed: false, pathConsumed: true));
				if (m_ServiceRequestData.HasComponent(policeCar.m_TargetRequest))
				{
					e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(policeCar.m_TargetRequest, Entity.Null, completed: true));
				}
				if (m_PathElements.HasBuffer(request))
				{
					DynamicBuffer<PathElement> appendPath = m_PathElements[request];
					if (appendPath.Length != 0)
					{
						DynamicBuffer<PathElement> dynamicBuffer = m_PathElements[vehicleEntity];
						PathUtils.TrimPath(dynamicBuffer, ref pathOwner);
						float num = policeCar.m_PathElementTime * (float)dynamicBuffer.Length + m_PathInformationData[request].m_Duration;
						if (PathUtils.TryAppendPath(ref currentLane, navigationLanes, dynamicBuffer, appendPath, m_SlaveLaneData, m_OwnerData, m_SubLanes))
						{
							if ((policeCarFlags & PoliceCarFlags.AccidentTarget) != 0)
							{
								car.m_Flags &= ~CarFlags.AnyLaneTarget;
								car.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad | CarFlags.UsePublicTransportLanes;
							}
							else
							{
								int dispatchIndex = BumpDispachIndex(request);
								Entity entity2 = Entity.Null;
								for (int i = 0; i < dynamicBuffer.Length; i++)
								{
									PathElement pathElement = dynamicBuffer[i];
									if (m_EdgeLaneData.HasComponent(pathElement.m_Target))
									{
										Owner owner = m_OwnerData[pathElement.m_Target];
										if (owner.m_Owner != entity2)
										{
											entity2 = owner.m_Owner;
											AddPatrolRequests(owner.m_Owner, request, dispatchIndex);
										}
									}
								}
								car.m_Flags &= ~CarFlags.Emergency;
								car.m_Flags |= CarFlags.StayOnRoad | CarFlags.AnyLaneTarget | CarFlags.UsePublicTransportLanes;
							}
							if (policeCar.m_RequestCount == 1)
							{
								policeCar.m_EstimatedShift = (uint)Mathf.RoundToInt(num * 3.75f);
							}
							policeCar.m_PathElementTime = num / (float)math.max(1, dynamicBuffer.Length);
							target.m_Target = entity;
							VehicleUtils.ClearEndOfPath(ref currentLane, navigationLanes);
							m_CommandBuffer.AddComponent<EffectsUpdated>(jobIndex, vehicleEntity);
							return true;
						}
					}
				}
				VehicleUtils.SetTarget(ref pathOwner, ref target, entity);
				return true;
			}
			return false;
		}

		private void ReturnToStation(int jobIndex, Entity vehicleEntity, Owner ownerData, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.PoliceCar policeCar, ref Car carData, ref PathOwner pathOwnerData, ref Target targetData)
		{
			serviceDispatches.Clear();
			policeCar.m_RequestCount = 0;
			policeCar.m_EstimatedShift = 0u;
			policeCar.m_State &= ~(PoliceCarFlags.AccidentTarget | PoliceCarFlags.AtTarget | PoliceCarFlags.Cancelled);
			policeCar.m_State |= PoliceCarFlags.Returning;
			VehicleUtils.SetTarget(ref pathOwnerData, ref targetData, ownerData.m_Owner);
		}

		private void ResetPath(int jobIndex, Entity vehicleEntity, ref Unity.Mathematics.Random random, PathInformation pathInformation, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.PoliceCar policeCar, ref Car carData, ref CarCurrentLane currentLane, ref PathOwner pathOwner)
		{
			DynamicBuffer<PathElement> path = m_PathElements[vehicleEntity];
			PathUtils.ResetPath(ref currentLane, path, m_SlaveLaneData, m_OwnerData, m_SubLanes);
			VehicleUtils.ResetParkingLaneStatus(vehicleEntity, ref currentLane, ref pathOwner, path, ref m_EntityLookup, ref m_CurveData, ref m_ParkingLaneData, ref m_CarLaneData, ref m_ConnectionLaneData, ref m_SpawnLocationData, ref m_PrefabRefData, ref m_PrefabSpawnLocationData);
			VehicleUtils.SetParkingCurvePos(vehicleEntity, ref random, currentLane, pathOwner, path, ref m_ParkedCarData, ref m_UnspawnedData, ref m_CurveData, ref m_ParkingLaneData, ref m_ConnectionLaneData, ref m_PrefabRefData, ref m_PrefabObjectGeometryData, ref m_PrefabParkingLaneData, ref m_LaneObjects, ref m_LaneOverlaps, ignoreDriveways: false);
			if ((policeCar.m_State & PoliceCarFlags.Returning) == 0 && policeCar.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				if (m_PolicePatrolRequestData.HasComponent(request))
				{
					Entity entity = Entity.Null;
					int dispatchIndex = BumpDispachIndex(request);
					for (int i = 0; i < path.Length; i++)
					{
						PathElement pathElement = path[i];
						if (m_EdgeLaneData.HasComponent(pathElement.m_Target))
						{
							Owner owner = m_OwnerData[pathElement.m_Target];
							if (owner.m_Owner != entity)
							{
								entity = owner.m_Owner;
								AddPatrolRequests(owner.m_Owner, request, dispatchIndex);
							}
						}
					}
					carData.m_Flags &= ~CarFlags.Emergency;
					carData.m_Flags |= CarFlags.StayOnRoad | CarFlags.AnyLaneTarget;
				}
				else if (m_PoliceEmergencyRequestData.HasComponent(request))
				{
					carData.m_Flags &= ~CarFlags.AnyLaneTarget;
					carData.m_Flags |= CarFlags.Emergency | CarFlags.StayOnRoad;
				}
				else
				{
					carData.m_Flags &= ~(CarFlags.Emergency | CarFlags.AnyLaneTarget);
					carData.m_Flags |= CarFlags.StayOnRoad;
				}
				if (policeCar.m_RequestCount == 1)
				{
					policeCar.m_EstimatedShift = (uint)Mathf.RoundToInt(pathInformation.m_Duration * 3.75f);
				}
			}
			else
			{
				carData.m_Flags &= ~(CarFlags.Emergency | CarFlags.StayOnRoad | CarFlags.AnyLaneTarget);
			}
			carData.m_Flags |= CarFlags.UsePublicTransportLanes;
			policeCar.m_PathElementTime = pathInformation.m_Duration / (float)math.max(1, path.Length);
			m_CommandBuffer.AddComponent<EffectsUpdated>(jobIndex, vehicleEntity);
		}

		private void AddPatrolRequests(Entity edgeEntity, Entity request, int dispatchIndex)
		{
			if (!m_ConnectedBuildings.HasBuffer(edgeEntity))
			{
				return;
			}
			DynamicBuffer<ConnectedBuilding> dynamicBuffer = m_ConnectedBuildings[edgeEntity];
			for (int i = 0; i < dynamicBuffer.Length; i++)
			{
				Entity building = dynamicBuffer[i].m_Building;
				if (m_CrimeProducerData.HasComponent(building))
				{
					m_ActionQueue.Enqueue(new PoliceAction
					{
						m_Type = PoliceActionType.AddPatrolRequest,
						m_Target = building,
						m_Request = request,
						m_DispatchIndex = dispatchIndex
					});
				}
			}
		}

		private void TryReduceCrime(Entity vehicleEntity, PoliceCarData prefabPoliceCarData, ref Target target)
		{
			TryReduceCrime(target.m_Target, prefabPoliceCarData.m_CrimeReductionRate);
		}

		private void TryReduceCrime(Entity building, float reduction)
		{
			if (m_CrimeProducerData.TryGetComponent(building, out var componentData) && componentData.m_Crime > 0f)
			{
				m_ActionQueue.Enqueue(new PoliceAction
				{
					m_Type = PoliceActionType.ReduceCrime,
					m_Target = building,
					m_CrimeReductionRate = reduction
				});
			}
		}

		private void TryReduceCrime(Entity vehicleEntity, PoliceCarData prefabPoliceCarData, ref CarCurrentLane currentLane, DynamicBuffer<CarNavigationLane> navigationLanes)
		{
			if (!m_EdgeLaneData.TryGetComponent(currentLane.m_Lane, out var componentData) || !m_OwnerData.TryGetComponent(currentLane.m_Lane, out var componentData2))
			{
				return;
			}
			for (int i = 0; i < navigationLanes.Length; i++)
			{
				CarNavigationLane value = navigationLanes[i];
				if ((value.m_Flags & Game.Vehicles.CarLaneFlags.Checked) != 0 || !m_OwnerData.TryGetComponent(value.m_Lane, out var componentData3) || componentData3.m_Owner != componentData2.m_Owner)
				{
					break;
				}
				value.m_Flags |= Game.Vehicles.CarLaneFlags.Checked;
				navigationLanes[i] = value;
			}
			if ((currentLane.m_LaneFlags & Game.Vehicles.CarLaneFlags.Checked) != 0)
			{
				return;
			}
			currentLane.m_LaneFlags |= Game.Vehicles.CarLaneFlags.Checked;
			float crimeReductionRate = prefabPoliceCarData.m_CrimeReductionRate;
			crimeReductionRate *= math.abs(componentData.m_EdgeDelta.y - componentData.m_EdgeDelta.x);
			if (m_ConnectedBuildings.TryGetBuffer(componentData2.m_Owner, out var bufferData))
			{
				for (int j = 0; j < bufferData.Length; j++)
				{
					TryReduceCrime(bufferData[j].m_Building, crimeReductionRate);
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct PoliceActionJob : IJob
	{
		[ReadOnly]
		public uint m_SimulationFrame;

		public ComponentLookup<PolicePatrolRequest> m_PolicePatrolRequestData;

		public ComponentLookup<CrimeProducer> m_CrimeProducerData;

		public ComponentLookup<AccidentSite> m_AccidentSiteData;

		public NativeQueue<PoliceAction> m_ActionQueue;

		public void Execute()
		{
			PoliceAction item;
			while (m_ActionQueue.TryDequeue(out item))
			{
				switch (item.m_Type)
				{
				case PoliceActionType.ReduceCrime:
				{
					CrimeProducer value3 = m_CrimeProducerData[item.m_Target];
					float num = math.min(item.m_CrimeReductionRate, value3.m_Crime);
					if (num > 0f)
					{
						value3.m_Crime -= num;
						m_CrimeProducerData[item.m_Target] = value3;
					}
					break;
				}
				case PoliceActionType.AddPatrolRequest:
				{
					CrimeProducer value4 = m_CrimeProducerData[item.m_Target];
					value4.m_PatrolRequest = item.m_Request;
					value4.m_DispatchIndex = (byte)item.m_DispatchIndex;
					m_CrimeProducerData[item.m_Target] = value4;
					break;
				}
				case PoliceActionType.SecureAccidentSite:
				{
					AccidentSite value2 = m_AccidentSiteData[item.m_Target];
					if ((value2.m_Flags & AccidentSiteFlags.Secured) == 0)
					{
						value2.m_Flags |= AccidentSiteFlags.Secured;
						value2.m_SecuredFrame = m_SimulationFrame;
					}
					m_AccidentSiteData[item.m_Target] = value2;
					break;
				}
				case PoliceActionType.BumpDispatchIndex:
				{
					PolicePatrolRequest value = m_PolicePatrolRequestData[item.m_Request];
					value.m_DispatchIndex++;
					m_PolicePatrolRequestData[item.m_Request] = value;
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
		public ComponentTypeHandle<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Stopped> __Game_Objects_Stopped_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<Passenger> __Game_Vehicles_Passenger_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Vehicles.PoliceCar> __Game_Vehicles_PoliceCar_RW_ComponentTypeHandle;

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
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Unspawned> __Game_Objects_Unspawned_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<EdgeLane> __Game_Net_EdgeLane_RO_ComponentLookup;

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
		public ComponentLookup<PoliceCarData> __Game_Prefabs_PoliceCarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingLaneData> __Game_Prefabs_ParkingLaneData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PolicePatrolRequest> __Game_Simulation_PolicePatrolRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PoliceEmergencyRequest> __Game_Simulation_PoliceEmergencyRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CrimeProducer> __Game_Buildings_CrimeProducer_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.PoliceStation> __Game_Buildings_PoliceStation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AccidentSite> __Game_Events_AccidentSite_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<InvolvedInAccident> __Game_Events_InvolvedInAccident_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<ConnectedBuilding> __Game_Buildings_ConnectedBuilding_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.SubLane> __Game_Net_SubLane_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneObject> __Game_Net_LaneObject_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LaneOverlap> __Game_Net_LaneOverlap_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<TargetElement> __Game_Events_TargetElement_RO_BufferLookup;

		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RW_BufferLookup;

		public ComponentLookup<PolicePatrolRequest> __Game_Simulation_PolicePatrolRequest_RW_ComponentLookup;

		public ComponentLookup<CrimeProducer> __Game_Buildings_CrimeProducer_RW_ComponentLookup;

		public ComponentLookup<AccidentSite> __Game_Events_AccidentSite_RW_ComponentLookup;

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
			__Game_Vehicles_Passenger_RO_BufferTypeHandle = state.GetBufferTypeHandle<Passenger>(isReadOnly: true);
			__Game_Vehicles_PoliceCar_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.PoliceCar>();
			__Game_Vehicles_Car_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Car>();
			__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<CarCurrentLane>();
			__Game_Common_Target_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Target>();
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<CarNavigationLane>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Common_Owner_RO_ComponentLookup = state.GetComponentLookup<Owner>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentLookup = state.GetComponentLookup<Unspawned>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Net_EdgeLane_RO_ComponentLookup = state.GetComponentLookup<EdgeLane>(isReadOnly: true);
			__Game_Net_CarLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.CarLane>(isReadOnly: true);
			__Game_Net_SlaveLane_RO_ComponentLookup = state.GetComponentLookup<SlaveLane>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Net_GarageLane_RO_ComponentLookup = state.GetComponentLookup<GarageLane>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Prefabs_CarData_RO_ComponentLookup = state.GetComponentLookup<CarData>(isReadOnly: true);
			__Game_Prefabs_PoliceCarData_RO_ComponentLookup = state.GetComponentLookup<PoliceCarData>(isReadOnly: true);
			__Game_Prefabs_ParkingLaneData_RO_ComponentLookup = state.GetComponentLookup<ParkingLaneData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Simulation_ServiceRequest_RO_ComponentLookup = state.GetComponentLookup<ServiceRequest>(isReadOnly: true);
			__Game_Simulation_PolicePatrolRequest_RO_ComponentLookup = state.GetComponentLookup<PolicePatrolRequest>(isReadOnly: true);
			__Game_Simulation_PoliceEmergencyRequest_RO_ComponentLookup = state.GetComponentLookup<PoliceEmergencyRequest>(isReadOnly: true);
			__Game_Buildings_CrimeProducer_RO_ComponentLookup = state.GetComponentLookup<CrimeProducer>(isReadOnly: true);
			__Game_Buildings_PoliceStation_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.PoliceStation>(isReadOnly: true);
			__Game_Events_AccidentSite_RO_ComponentLookup = state.GetComponentLookup<AccidentSite>(isReadOnly: true);
			__Game_Events_InvolvedInAccident_RO_ComponentLookup = state.GetComponentLookup<InvolvedInAccident>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Buildings_ConnectedBuilding_RO_BufferLookup = state.GetBufferLookup<ConnectedBuilding>(isReadOnly: true);
			__Game_Net_SubLane_RO_BufferLookup = state.GetBufferLookup<Game.Net.SubLane>(isReadOnly: true);
			__Game_Net_LaneObject_RO_BufferLookup = state.GetBufferLookup<LaneObject>(isReadOnly: true);
			__Game_Net_LaneOverlap_RO_BufferLookup = state.GetBufferLookup<LaneOverlap>(isReadOnly: true);
			__Game_Events_TargetElement_RO_BufferLookup = state.GetBufferLookup<TargetElement>(isReadOnly: true);
			__Game_Pathfind_PathElement_RW_BufferLookup = state.GetBufferLookup<PathElement>();
			__Game_Simulation_PolicePatrolRequest_RW_ComponentLookup = state.GetComponentLookup<PolicePatrolRequest>();
			__Game_Buildings_CrimeProducer_RW_ComponentLookup = state.GetComponentLookup<CrimeProducer>();
			__Game_Events_AccidentSite_RW_ComponentLookup = state.GetComponentLookup<AccidentSite>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private EntityQuery m_VehicleQuery;

	private EntityArchetype m_PolicePatrolRequestArchetype;

	private EntityArchetype m_PoliceEmergencyRequestArchetype;

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
		return 5;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadWrite<CarCurrentLane>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadWrite<Game.Vehicles.PoliceCar>(), ComponentType.ReadWrite<Car>(), ComponentType.ReadWrite<Target>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>(), ComponentType.Exclude<OutOfControl>());
		m_PolicePatrolRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<PolicePatrolRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_PoliceEmergencyRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<PoliceEmergencyRequest>(), ComponentType.ReadWrite<RequestGroup>());
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
		NativeQueue<PoliceAction> actionQueue = new NativeQueue<PoliceAction>(Allocator.TempJob);
		PoliceCarTickJob jobData = new PoliceCarTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StoppedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Stopped_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PassengerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_Passenger_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PoliceCarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_PoliceCar_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Car_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_CarCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CarNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_CarNavigationLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_OwnerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Common_Owner_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_UnspawnedData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_EdgeLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_EdgeLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CarLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_CarLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SlaveLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_SlaveLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_GarageLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_GarageLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPoliceCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PoliceCarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingLaneData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PolicePatrolRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PolicePatrolRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PoliceEmergencyRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PoliceEmergencyRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CrimeProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PoliceStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PoliceStation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AccidentSiteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_AccidentSite_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InvolvedInAccidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InvolvedInAccident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectedBuildings = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Buildings_ConnectedBuilding_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubLanes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_SubLane_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneObjects = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneObject_RO_BufferLookup, ref base.CheckedStateRef),
			m_LaneOverlaps = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_LaneOverlap_RO_BufferLookup, ref base.CheckedStateRef),
			m_TargetElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Events_TargetElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_SimulationFrameIndex = m_SimulationSystem.frameIndex,
			m_RandomSeed = RandomSeed.Next(),
			m_PolicePatrolRequestArchetype = m_PolicePatrolRequestArchetype,
			m_PoliceEmergencyRequestArchetype = m_PoliceEmergencyRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_MovingToParkedCarRemoveTypes = m_MovingToParkedCarRemoveTypes,
			m_MovingToParkedAddTypes = m_MovingToParkedAddTypes,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
			m_ActionQueue = actionQueue.AsParallelWriter()
		};
		PoliceActionJob jobData2 = new PoliceActionJob
		{
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_PolicePatrolRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PolicePatrolRequest_RW_ComponentLookup, ref base.CheckedStateRef),
			m_CrimeProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CrimeProducer_RW_ComponentLookup, ref base.CheckedStateRef),
			m_AccidentSiteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_AccidentSite_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ActionQueue = actionQueue
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_VehicleQuery, base.Dependency);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		actionQueue.Dispose(jobHandle2);
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
	public PoliceCarAISystem()
	{
	}
}
