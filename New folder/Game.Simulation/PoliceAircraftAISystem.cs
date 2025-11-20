using System.Runtime.CompilerServices;
using Colossal.Collections;
using Colossal.Mathematics;
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
public class PoliceAircraftAISystem : GameSystemBase
{
	private struct PoliceAction
	{
		public PoliceActionType m_Type;

		public Entity m_Target;

		public Entity m_Request;

		public float m_CrimeReductionRate;
	}

	private enum PoliceActionType
	{
		ReduceCrime,
		AddPatrolRequest,
		SecureAccidentSite
	}

	[BurstCompile]
	private struct PoliceAircraftTickJob : IJobChunk
	{
		private struct FindPointOfInterestIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Circle2 m_Circle;

			public Unity.Mathematics.Random m_Random;

			public float3 m_Result;

			public int m_TotalProbability;

			public ComponentLookup<Game.Objects.Transform> m_TransformData;

			public ComponentLookup<CrimeProducer> m_CrimeProducerData;

			public ComponentLookup<PrefabRef> m_PrefabRefData;

			public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds.xz, m_Circle);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity building)
			{
				if (!MathUtils.Intersect(bounds.m_Bounds.xz, m_Circle) || !m_CrimeProducerData.HasComponent(building))
				{
					return;
				}
				PrefabRef prefabRef = m_PrefabRefData[building];
				float3 result = ObjectUtils.LocalToWorld(position: new float3(0f, 0f, m_PrefabObjectGeometryData[prefabRef.m_Prefab].m_Bounds.max.z), transform: m_TransformData[building]);
				if (math.distance(m_Circle.position, result.xz) < m_Circle.radius)
				{
					int num = 100;
					m_TotalProbability += num;
					if (m_Random.NextInt(m_TotalProbability) < num)
					{
						m_Result = result;
					}
				}
			}
		}

		private struct AddRequestIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public Bezier4x2 m_Curve;

			public float m_Distance;

			public Entity m_Request;

			public ComponentLookup<Game.Objects.Transform> m_TransformData;

			public ComponentLookup<CrimeProducer> m_CrimeProducerData;

			public NativeQueue<PoliceAction>.ParallelWriter m_ActionQueue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity building)
			{
				if (MathUtils.Intersect(bounds.m_Bounds, m_Bounds) && m_CrimeProducerData.HasComponent(building))
				{
					Game.Objects.Transform transform = m_TransformData[building];
					if (!(MathUtils.Distance(m_Curve, transform.m_Position.xz, out var _) > m_Distance))
					{
						m_ActionQueue.Enqueue(new PoliceAction
						{
							m_Type = PoliceActionType.AddPatrolRequest,
							m_Target = building,
							m_Request = m_Request
						});
					}
				}
			}
		}

		private struct ReduceCrimeIterator : INativeQuadTreeIterator<Entity, QuadTreeBoundsXZ>, IUnsafeQuadTreeIterator<Entity, QuadTreeBoundsXZ>
		{
			public Bounds3 m_Bounds;

			public Bezier4x2 m_Curve;

			public float2 m_Distance;

			public float m_Reduction;

			public ComponentLookup<Game.Objects.Transform> m_TransformData;

			public ComponentLookup<CrimeProducer> m_CrimeProducerData;

			public NativeQueue<PoliceAction>.ParallelWriter m_ActionQueue;

			public bool Intersect(QuadTreeBoundsXZ bounds)
			{
				return MathUtils.Intersect(bounds.m_Bounds, m_Bounds);
			}

			public void Iterate(QuadTreeBoundsXZ bounds, Entity building)
			{
				if (MathUtils.Intersect(bounds.m_Bounds, m_Bounds) && m_CrimeProducerData.HasComponent(building))
				{
					Game.Objects.Transform transform = m_TransformData[building];
					float t;
					float num = MathUtils.Distance(m_Curve, transform.m_Position.xz, out t);
					if (!(num >= m_Distance.y) && m_CrimeProducerData[building].m_Crime > 0f)
					{
						m_ActionQueue.Enqueue(new PoliceAction
						{
							m_Type = PoliceActionType.ReduceCrime,
							m_Target = building,
							m_CrimeReductionRate = m_Reduction * (1f - math.max(0f, num - m_Distance.x) / (m_Distance.y - m_Distance.x))
						});
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

		[ReadOnly]
		public BufferTypeHandle<Passenger> m_PassengerType;

		public ComponentTypeHandle<Game.Vehicles.PoliceCar> m_PoliceCarType;

		public ComponentTypeHandle<Aircraft> m_AircraftType;

		public ComponentTypeHandle<AircraftCurrentLane> m_CurrentLaneType;

		public ComponentTypeHandle<Target> m_TargetType;

		public ComponentTypeHandle<PointOfInterest> m_PointOfInterestType;

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
		public ComponentLookup<PoliceCarData> m_PrefabPoliceCarData;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> m_PrefabObjectGeometryData;

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
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Curve> m_CurveData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> m_ConnectionLaneData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Blocker> m_BlockerData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public uint m_SimulationFrameIndex;

		[ReadOnly]
		public EntityArchetype m_PolicePatrolRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_PoliceEmergencyRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedAircraftRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedAddTypes;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public NativeQuadTree<Entity, QuadTreeBoundsXZ> m_ObjectSearchTree;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public NativeQueue<PoliceAction>.ParallelWriter m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<PathInformation> nativeArray4 = chunk.GetNativeArray(ref m_PathInformationType);
			NativeArray<AircraftCurrentLane> nativeArray5 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<Game.Vehicles.PoliceCar> nativeArray6 = chunk.GetNativeArray(ref m_PoliceCarType);
			NativeArray<Aircraft> nativeArray7 = chunk.GetNativeArray(ref m_AircraftType);
			NativeArray<Target> nativeArray8 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<PointOfInterest> nativeArray9 = chunk.GetNativeArray(ref m_PointOfInterestType);
			NativeArray<PathOwner> nativeArray10 = chunk.GetNativeArray(ref m_PathOwnerType);
			BufferAccessor<Passenger> bufferAccessor = chunk.GetBufferAccessor(ref m_PassengerType);
			BufferAccessor<AircraftNavigationLane> bufferAccessor2 = chunk.GetBufferAccessor(ref m_AircraftNavigationLaneType);
			BufferAccessor<ServiceDispatch> bufferAccessor3 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Owner owner = nativeArray2[i];
				PrefabRef prefabRef = nativeArray3[i];
				PathInformation pathInformation = nativeArray4[i];
				Game.Vehicles.PoliceCar policeCar = nativeArray6[i];
				Aircraft aircraft = nativeArray7[i];
				AircraftCurrentLane currentLane = nativeArray5[i];
				PathOwner pathOwner = nativeArray10[i];
				Target target = nativeArray8[i];
				PointOfInterest pointOfInterest = nativeArray9[i];
				DynamicBuffer<AircraftNavigationLane> navigationLanes = bufferAccessor2[i];
				DynamicBuffer<ServiceDispatch> serviceDispatches = bufferAccessor3[i];
				DynamicBuffer<Passenger> passengers = default(DynamicBuffer<Passenger>);
				if (bufferAccessor.Length != 0)
				{
					passengers = bufferAccessor[i];
				}
				VehicleUtils.CheckUnspawned(unfilteredChunkIndex, entity, currentLane, isUnspawned, m_CommandBuffer);
				Tick(unfilteredChunkIndex, ref random, entity, owner, prefabRef, pathInformation, passengers, navigationLanes, serviceDispatches, ref policeCar, ref aircraft, ref currentLane, ref pathOwner, ref target, ref pointOfInterest);
				nativeArray6[i] = policeCar;
				nativeArray7[i] = aircraft;
				nativeArray5[i] = currentLane;
				nativeArray10[i] = pathOwner;
				nativeArray8[i] = target;
				nativeArray9[i] = pointOfInterest;
			}
		}

		private void Tick(int jobIndex, ref Unity.Mathematics.Random random, Entity vehicleEntity, Owner owner, PrefabRef prefabRef, PathInformation pathInformation, DynamicBuffer<Passenger> passengers, DynamicBuffer<AircraftNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.PoliceCar policeCar, ref Aircraft aircraft, ref AircraftCurrentLane currentLane, ref PathOwner pathOwner, ref Target target, ref PointOfInterest pointOfInterest)
		{
			PoliceCarData prefabPoliceCarData = m_PrefabPoliceCarData[prefabRef.m_Prefab];
			policeCar.m_EstimatedShift = math.select(policeCar.m_EstimatedShift - 1, 0u, policeCar.m_EstimatedShift == 0);
			if (++policeCar.m_ShiftTime >= prefabPoliceCarData.m_ShiftDuration)
			{
				policeCar.m_State |= PoliceCarFlags.ShiftEnded;
			}
			if ((currentLane.m_LaneFlags & (AircraftLaneFlags.Flying | AircraftLaneFlags.Landing | AircraftLaneFlags.TakingOff)) == AircraftLaneFlags.Flying)
			{
				UpdatePointOfInterest(vehicleEntity, ref random, ref policeCar, ref aircraft, ref target, ref pointOfInterest);
			}
			else
			{
				pointOfInterest.m_IsValid = false;
				aircraft.m_Flags &= ~AircraftFlags.Working;
			}
			if ((aircraft.m_Flags & AircraftFlags.Emergency) == 0)
			{
				TryReduceCrime(vehicleEntity, prefabPoliceCarData, ref currentLane);
			}
			if (VehicleUtils.ResetUpdatedPath(ref pathOwner))
			{
				ResetPath(jobIndex, vehicleEntity, pathInformation, serviceDispatches, ref policeCar, ref aircraft, ref currentLane);
			}
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
				if (VehicleUtils.IsStuck(pathOwner) || (policeCar.m_State & PoliceCarFlags.Returning) != 0)
				{
					m_CommandBuffer.AddComponent(jobIndex, vehicleEntity, default(Deleted));
					return;
				}
				ReturnToDepot(owner, serviceDispatches, ref policeCar, ref aircraft, ref pathOwner, ref target);
			}
			else if (VehicleUtils.PathEndReached(currentLane) || (policeCar.m_State & (PoliceCarFlags.AtTarget | PoliceCarFlags.Disembarking)) != 0)
			{
				if ((policeCar.m_State & PoliceCarFlags.Returning) != 0)
				{
					if ((policeCar.m_State & PoliceCarFlags.Disembarking) != 0)
					{
						if (StopDisembarking(passengers, ref policeCar))
						{
							if (VehicleUtils.ParkingSpaceReached(currentLane, pathOwner))
							{
								ParkAircraft(jobIndex, vehicleEntity, owner, ref aircraft, ref policeCar, ref currentLane);
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
							ParkAircraft(jobIndex, vehicleEntity, owner, ref aircraft, ref policeCar, ref currentLane);
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
					flag &= SecureAccidentSite(ref policeCar, passengers, serviceDispatches);
				}
				else
				{
					TryReduceCrime(vehicleEntity, prefabPoliceCarData, ref target);
				}
				if (flag)
				{
					CheckServiceDispatches(vehicleEntity, serviceDispatches, passengers, ref policeCar, ref pathOwner);
					if ((policeCar.m_State & (PoliceCarFlags.ShiftEnded | PoliceCarFlags.Disabled)) != 0 || !SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, passengers, ref policeCar, ref aircraft, ref currentLane, ref pathOwner, ref target))
					{
						ReturnToDepot(owner, serviceDispatches, ref policeCar, ref aircraft, ref pathOwner, ref target);
					}
				}
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
			if ((aircraft.m_Flags & AircraftFlags.Emergency) == 0 && (policeCar.m_State & (PoliceCarFlags.ShiftEnded | PoliceCarFlags.Disabled)) != 0)
			{
				if ((policeCar.m_State & PoliceCarFlags.Returning) == 0)
				{
					ReturnToDepot(owner, serviceDispatches, ref policeCar, ref aircraft, ref pathOwner, ref target);
				}
				serviceDispatches.Clear();
			}
			else
			{
				CheckServiceDispatches(vehicleEntity, serviceDispatches, passengers, ref policeCar, ref pathOwner);
				if ((policeCar.m_State & (PoliceCarFlags.Returning | PoliceCarFlags.Cancelled)) != 0)
				{
					SelectNextDispatch(jobIndex, vehicleEntity, navigationLanes, serviceDispatches, passengers, ref policeCar, ref aircraft, ref currentLane, ref pathOwner, ref target);
				}
				if (policeCar.m_RequestCount <= 1 && (policeCar.m_State & (PoliceCarFlags.ShiftEnded | PoliceCarFlags.Disabled)) == 0)
				{
					RequestTargetIfNeeded(jobIndex, vehicleEntity, ref policeCar);
				}
			}
			if ((policeCar.m_State & (PoliceCarFlags.AtTarget | PoliceCarFlags.Disembarking)) == 0)
			{
				if (VehicleUtils.RequireNewPath(pathOwner))
				{
					FindNewPath(vehicleEntity, prefabRef, ref policeCar, ref aircraft, ref currentLane, ref pathOwner, ref target);
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

		private void ParkAircraft(int jobIndex, Entity entity, Owner owner, ref Aircraft aircraft, ref Game.Vehicles.PoliceCar policeCar, ref AircraftCurrentLane currentLane)
		{
			aircraft.m_Flags &= ~(AircraftFlags.Emergency | AircraftFlags.IgnoreParkedVehicle);
			policeCar.m_State = PoliceCarFlags.Empty;
			if (m_PoliceStationData.TryGetComponent(owner.m_Owner, out var componentData) && (componentData.m_Flags & PoliceStationFlags.HasAvailablePoliceHelicopters) == 0)
			{
				policeCar.m_State |= PoliceCarFlags.Disabled;
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

		private void UpdatePointOfInterest(Entity entity, ref Unity.Mathematics.Random random, ref Game.Vehicles.PoliceCar policeCar, ref Aircraft aircraft, ref Target target, ref PointOfInterest pointOfInterest)
		{
			Game.Objects.Transform transform = m_TransformData[entity];
			if (m_TransformData.TryGetComponent(target.m_Target, out var componentData))
			{
				if (m_CrimeProducerData.HasComponent(target.m_Target))
				{
					PrefabRef prefabRef = m_PrefabRefData[target.m_Target];
					componentData.m_Position = ObjectUtils.LocalToWorld(position: new float3(0f, 0f, m_PrefabObjectGeometryData[prefabRef.m_Prefab].m_Bounds.max.z), transform: componentData);
				}
				if (math.distancesq(transform.m_Position.xz, componentData.m_Position.xz) < 40000f)
				{
					pointOfInterest.m_Position = componentData.m_Position;
					pointOfInterest.m_IsValid = true;
					aircraft.m_Flags |= AircraftFlags.Working;
					return;
				}
			}
			if ((aircraft.m_Flags & AircraftFlags.Emergency) == 0)
			{
				float3 value = math.forward(transform.m_Rotation);
				value = MathUtils.Normalize(value, value.xz);
				float3 @float = transform.m_Position + value * 50f;
				if (pointOfInterest.m_IsValid && math.distancesq(@float.xz, pointOfInterest.m_Position.xz) < 40000f)
				{
					aircraft.m_Flags |= AircraftFlags.Working;
					return;
				}
				float num = 125f;
				FindPointOfInterestIterator iterator = new FindPointOfInterestIterator
				{
					m_Circle = new Circle2(num, transform.m_Position.xz + value.xz * num),
					m_Random = random,
					m_TransformData = m_TransformData,
					m_CrimeProducerData = m_CrimeProducerData,
					m_PrefabRefData = m_PrefabRefData,
					m_PrefabObjectGeometryData = m_PrefabObjectGeometryData
				};
				m_ObjectSearchTree.Iterate(ref iterator);
				random = iterator.m_Random;
				if (iterator.m_TotalProbability != 0)
				{
					pointOfInterest.m_Position = iterator.m_Result;
					pointOfInterest.m_IsValid = true;
					aircraft.m_Flags |= AircraftFlags.Working;
					return;
				}
			}
			pointOfInterest.m_IsValid = false;
			aircraft.m_Flags &= ~AircraftFlags.Working;
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
			policeCar.m_State &= ~PoliceCarFlags.Disembarking;
			return true;
		}

		private void FindNewPath(Entity vehicleEntity, PrefabRef prefabRef, ref Game.Vehicles.PoliceCar policeCar, ref Aircraft aircraft, ref AircraftCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			HelicopterData helicopterData = m_PrefabHelicopterData[prefabRef.m_Prefab];
			PathfindParameters parameters = new PathfindParameters
			{
				m_MaxSpeed = helicopterData.m_FlyingMaxSpeed,
				m_WalkSpeed = 5.555556f,
				m_Methods = (PathMethod.Road | PathMethod.Flying),
				m_IgnoredRules = (RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
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
			if ((policeCar.m_State & PoliceCarFlags.AccidentTarget) != 0)
			{
				destination.m_Type = SetupTargetType.AccidentLocation;
				destination.m_Value2 = 30f;
				destination.m_Methods = PathMethod.Flying;
				destination.m_FlyingTypes = RoadTypes.Helicopter;
			}
			else if ((policeCar.m_State & PoliceCarFlags.Returning) == 0)
			{
				destination.m_Methods = PathMethod.Flying;
				destination.m_FlyingTypes = RoadTypes.Helicopter;
			}
			else
			{
				destination.m_Methods = PathMethod.Road;
				destination.m_RoadTypes = RoadTypes.Helicopter;
			}
			if ((policeCar.m_State & PoliceCarFlags.Returning) == 0)
			{
				parameters.m_Weights = new PathfindWeights(1f, 0f, 0f, 0f);
			}
			else
			{
				parameters.m_Weights = new PathfindWeights(1f, 1f, 1f, 1f);
				destination.m_RandomCost = 30f;
			}
			VehicleUtils.SetupPathfind(item: new SetupQueueItem(vehicleEntity, parameters, origin, destination), currentLane: ref currentLane, pathOwner: ref pathOwner, queue: m_PathfindQueue);
		}

		private bool SecureAccidentSite(ref Game.Vehicles.PoliceCar policeCar, DynamicBuffer<Passenger> passengers, DynamicBuffer<ServiceDispatch> serviceDispatches)
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
					if (m_EntityLookup.Exists(policePatrolRequest.m_Target) && !flag3 && policePatrolRequest.m_Priority > num)
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
					if (m_EntityLookup.Exists(policeEmergencyRequest.m_Site) && (!flag3 || policeEmergencyRequest.m_Priority > num))
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

		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Vehicles.PoliceCar policeCar)
		{
			if (!m_ServiceRequestData.HasComponent(policeCar.m_TargetRequest) && (policeCar.m_PurposeMask & PolicePurpose.Patrol) != 0 && (policeCar.m_State & (PoliceCarFlags.Empty | PoliceCarFlags.EstimatedShiftEnd)) == PoliceCarFlags.Empty)
			{
				uint num = math.max(512u, 16u);
				if ((m_SimulationFrameIndex & (num - 1)) == 10)
				{
					Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_PolicePatrolRequestArchetype);
					m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
					m_CommandBuffer.SetComponent(jobIndex, e, new PolicePatrolRequest(entity, 1f));
					m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(32u));
				}
			}
		}

		private bool SelectNextDispatch(int jobIndex, Entity vehicleEntity, DynamicBuffer<AircraftNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, DynamicBuffer<Passenger> passengers, ref Game.Vehicles.PoliceCar policeCar, ref Aircraft aircraft, ref AircraftCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
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
				if (!m_EntityLookup.Exists(entity))
				{
					serviceDispatches.RemoveAt(0);
					policeCar.m_EstimatedShift -= policeCar.m_EstimatedShift / (uint)policeCar.m_RequestCount;
					policeCar.m_RequestCount--;
					continue;
				}
				aircraft.m_Flags &= ~AircraftFlags.IgnoreParkedVehicle;
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
						if (PathUtils.TryAppendPath(ref currentLane, navigationLanes, dynamicBuffer, appendPath))
						{
							if ((policeCarFlags & PoliceCarFlags.AccidentTarget) != 0)
							{
								aircraft.m_Flags |= AircraftFlags.Emergency | AircraftFlags.StayMidAir;
							}
							else
							{
								for (int i = 0; i < dynamicBuffer.Length; i++)
								{
									PathElement pathElement = dynamicBuffer[i];
									if (m_ConnectionLaneData.HasComponent(pathElement.m_Target) && (m_ConnectionLaneData[pathElement.m_Target].m_Flags & (ConnectionLaneFlags.Outside | ConnectionLaneFlags.Airway)) == ConnectionLaneFlags.Airway)
									{
										AddPatrolRequests(pathElement.m_Target, request);
									}
								}
								aircraft.m_Flags &= ~AircraftFlags.Emergency;
								aircraft.m_Flags |= AircraftFlags.StayMidAir;
							}
							if (policeCar.m_RequestCount == 1)
							{
								policeCar.m_EstimatedShift = (uint)Mathf.RoundToInt(num * 3.75f);
							}
							policeCar.m_PathElementTime = num / (float)math.max(1, dynamicBuffer.Length);
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

		private void ReturnToDepot(Owner owner, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.PoliceCar policeCar, ref Aircraft aircraft, ref PathOwner pathOwner, ref Target target)
		{
			serviceDispatches.Clear();
			policeCar.m_RequestCount = 0;
			policeCar.m_EstimatedShift = 0u;
			policeCar.m_State &= ~(PoliceCarFlags.AccidentTarget | PoliceCarFlags.AtTarget | PoliceCarFlags.Cancelled);
			policeCar.m_State |= PoliceCarFlags.Returning;
			aircraft.m_Flags &= ~(AircraftFlags.Emergency | AircraftFlags.IgnoreParkedVehicle);
			VehicleUtils.SetTarget(ref pathOwner, ref target, owner.m_Owner);
		}

		private void ResetPath(int jobIndex, Entity vehicleEntity, PathInformation pathInformation, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.PoliceCar policeCar, ref Aircraft aircraft, ref AircraftCurrentLane currentLane)
		{
			DynamicBuffer<PathElement> path = m_PathElements[vehicleEntity];
			PathUtils.ResetPath(ref currentLane, path);
			if ((policeCar.m_State & PoliceCarFlags.Returning) == 0 && policeCar.m_RequestCount > 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				if (m_PolicePatrolRequestData.HasComponent(request))
				{
					for (int i = 0; i < path.Length; i++)
					{
						PathElement pathElement = path[i];
						if (m_ConnectionLaneData.HasComponent(pathElement.m_Target) && (m_ConnectionLaneData[pathElement.m_Target].m_Flags & (ConnectionLaneFlags.Outside | ConnectionLaneFlags.Airway)) == ConnectionLaneFlags.Airway)
						{
							AddPatrolRequests(pathElement.m_Target, request);
						}
					}
					aircraft.m_Flags &= ~AircraftFlags.Emergency;
					aircraft.m_Flags |= AircraftFlags.StayMidAir;
				}
				else if (m_PoliceEmergencyRequestData.HasComponent(request))
				{
					aircraft.m_Flags |= AircraftFlags.Emergency | AircraftFlags.StayMidAir;
				}
				else
				{
					aircraft.m_Flags &= ~AircraftFlags.Emergency;
					aircraft.m_Flags |= AircraftFlags.StayMidAir;
				}
				if (policeCar.m_RequestCount == 1)
				{
					policeCar.m_EstimatedShift = (uint)Mathf.RoundToInt(pathInformation.m_Duration * 3.75f);
				}
			}
			else
			{
				aircraft.m_Flags &= ~(AircraftFlags.StayOnTaxiway | AircraftFlags.StayMidAir);
			}
			policeCar.m_PathElementTime = pathInformation.m_Duration / (float)math.max(1, path.Length);
		}

		private void AddPatrolRequests(Entity laneEntity, Entity request)
		{
			Curve curve = m_CurveData[laneEntity];
			AddRequestIterator iterator = new AddRequestIterator
			{
				m_Bounds = MathUtils.Expand(MathUtils.Bounds(curve.m_Bezier), 300f),
				m_Curve = curve.m_Bezier.xz,
				m_Distance = 300f,
				m_Request = request,
				m_TransformData = m_TransformData,
				m_CrimeProducerData = m_CrimeProducerData,
				m_ActionQueue = m_ActionQueue
			};
			m_ObjectSearchTree.Iterate(ref iterator);
		}

		private void TryReduceCrime(Entity vehicleEntity, PoliceCarData prefabPoliceCarData, ref AircraftCurrentLane currentLane)
		{
			if (m_ConnectionLaneData.HasComponent(currentLane.m_Lane) && (m_ConnectionLaneData[currentLane.m_Lane].m_Flags & (ConnectionLaneFlags.Outside | ConnectionLaneFlags.Airway)) == ConnectionLaneFlags.Airway && (currentLane.m_LaneFlags & AircraftLaneFlags.Checked) == 0)
			{
				currentLane.m_LaneFlags |= AircraftLaneFlags.Checked;
				ReduceCrime(currentLane.m_Lane, prefabPoliceCarData.m_CrimeReductionRate);
			}
		}

		private void TryReduceCrime(Entity vehicleEntity, PoliceCarData prefabPoliceCarData, ref Target target)
		{
			if (m_CrimeProducerData.HasComponent(target.m_Target) && m_CrimeProducerData[target.m_Target].m_Crime > 0f)
			{
				m_ActionQueue.Enqueue(new PoliceAction
				{
					m_Type = PoliceActionType.ReduceCrime,
					m_Target = target.m_Target,
					m_CrimeReductionRate = prefabPoliceCarData.m_CrimeReductionRate
				});
			}
		}

		private void ReduceCrime(Entity laneEntity, float reduction)
		{
			Curve curve = m_CurveData[laneEntity];
			ReduceCrimeIterator iterator = new ReduceCrimeIterator
			{
				m_Bounds = MathUtils.Expand(MathUtils.Bounds(curve.m_Bezier), 450.00003f),
				m_Curve = curve.m_Bezier.xz,
				m_Distance = 750f * new float2(0.2f, 0.6f),
				m_Reduction = reduction,
				m_TransformData = m_TransformData,
				m_CrimeProducerData = m_CrimeProducerData,
				m_ActionQueue = m_ActionQueue
			};
			m_ObjectSearchTree.Iterate(ref iterator);
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
					CrimeProducer value2 = m_CrimeProducerData[item.m_Target];
					float num = math.min(item.m_CrimeReductionRate, value2.m_Crime);
					if (num > 0f)
					{
						value2.m_Crime -= num;
						m_CrimeProducerData[item.m_Target] = value2;
					}
					break;
				}
				case PoliceActionType.AddPatrolRequest:
				{
					CrimeProducer value3 = m_CrimeProducerData[item.m_Target];
					value3.m_PatrolRequest = item.m_Request;
					m_CrimeProducerData[item.m_Target] = value3;
					break;
				}
				case PoliceActionType.SecureAccidentSite:
				{
					AccidentSite value = m_AccidentSiteData[item.m_Target];
					if ((value.m_Flags & AccidentSiteFlags.Secured) == 0)
					{
						value.m_Flags |= AccidentSiteFlags.Secured;
						value.m_SecuredFrame = m_SimulationFrame;
					}
					m_AccidentSiteData[item.m_Target] = value;
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

		[ReadOnly]
		public BufferTypeHandle<Passenger> __Game_Vehicles_Passenger_RO_BufferTypeHandle;

		public ComponentTypeHandle<Game.Vehicles.PoliceCar> __Game_Vehicles_PoliceCar_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Aircraft> __Game_Vehicles_Aircraft_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Target> __Game_Common_Target_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PointOfInterest> __Game_Common_PointOfInterest_RW_ComponentTypeHandle;

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
		public ComponentLookup<PoliceCarData> __Game_Prefabs_PoliceCarData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ObjectGeometryData> __Game_Prefabs_ObjectGeometryData_RO_ComponentLookup;

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
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Curve> __Game_Net_Curve_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ConnectionLane> __Game_Net_ConnectionLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		public ComponentLookup<Blocker> __Game_Vehicles_Blocker_RW_ComponentLookup;

		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RW_BufferLookup;

		public ComponentLookup<CrimeProducer> __Game_Buildings_CrimeProducer_RW_ComponentLookup;

		public ComponentLookup<AccidentSite> __Game_Events_AccidentSite_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Vehicles_Passenger_RO_BufferTypeHandle = state.GetBufferTypeHandle<Passenger>(isReadOnly: true);
			__Game_Vehicles_PoliceCar_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.PoliceCar>();
			__Game_Vehicles_Aircraft_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Aircraft>();
			__Game_Vehicles_AircraftCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AircraftCurrentLane>();
			__Game_Common_Target_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Target>();
			__Game_Common_PointOfInterest_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PointOfInterest>();
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Vehicles_AircraftNavigationLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<AircraftNavigationLane>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Prefabs_HelicopterData_RO_ComponentLookup = state.GetComponentLookup<HelicopterData>(isReadOnly: true);
			__Game_Prefabs_PoliceCarData_RO_ComponentLookup = state.GetComponentLookup<PoliceCarData>(isReadOnly: true);
			__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup = state.GetComponentLookup<ObjectGeometryData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Simulation_ServiceRequest_RO_ComponentLookup = state.GetComponentLookup<ServiceRequest>(isReadOnly: true);
			__Game_Simulation_PolicePatrolRequest_RO_ComponentLookup = state.GetComponentLookup<PolicePatrolRequest>(isReadOnly: true);
			__Game_Simulation_PoliceEmergencyRequest_RO_ComponentLookup = state.GetComponentLookup<PoliceEmergencyRequest>(isReadOnly: true);
			__Game_Buildings_CrimeProducer_RO_ComponentLookup = state.GetComponentLookup<CrimeProducer>(isReadOnly: true);
			__Game_Buildings_PoliceStation_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.PoliceStation>(isReadOnly: true);
			__Game_Events_AccidentSite_RO_ComponentLookup = state.GetComponentLookup<AccidentSite>(isReadOnly: true);
			__Game_Events_InvolvedInAccident_RO_ComponentLookup = state.GetComponentLookup<InvolvedInAccident>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Net_Curve_RO_ComponentLookup = state.GetComponentLookup<Curve>(isReadOnly: true);
			__Game_Net_ConnectionLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ConnectionLane>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Vehicles_Blocker_RW_ComponentLookup = state.GetComponentLookup<Blocker>();
			__Game_Pathfind_PathElement_RW_BufferLookup = state.GetBufferLookup<PathElement>();
			__Game_Buildings_CrimeProducer_RW_ComponentLookup = state.GetComponentLookup<CrimeProducer>();
			__Game_Events_AccidentSite_RW_ComponentLookup = state.GetComponentLookup<AccidentSite>();
		}
	}

	private const float MAX_WORK_DISTANCE = 200f;

	private EndFrameBarrier m_EndFrameBarrier;

	private SimulationSystem m_SimulationSystem;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private Game.Objects.SearchSystem m_ObjectSearchSystem;

	private EntityQuery m_VehicleQuery;

	private EntityArchetype m_PolicePatrolRequestArchetype;

	private EntityArchetype m_PoliceEmergencyRequestArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private ComponentTypeSet m_MovingToParkedAircraftRemoveTypes;

	private ComponentTypeSet m_MovingToParkedAddTypes;

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
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadWrite<AircraftCurrentLane>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadWrite<Game.Vehicles.PoliceCar>(), ComponentType.ReadWrite<Target>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>(), ComponentType.Exclude<OutOfControl>());
		m_PolicePatrolRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<PolicePatrolRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_PoliceEmergencyRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<PoliceEmergencyRequest>(), ComponentType.ReadWrite<RequestGroup>());
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
		RequireForUpdate(m_VehicleQuery);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		NativeQueue<PoliceAction> actionQueue = new NativeQueue<PoliceAction>(Allocator.TempJob);
		JobHandle dependencies;
		PoliceAircraftTickJob jobData = new PoliceAircraftTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PassengerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_Passenger_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_PoliceCarType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_PoliceCar_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AircraftType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Aircraft_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PointOfInterestType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_PointOfInterest_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AircraftNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftNavigationLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabHelicopterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HelicopterData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPoliceCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PoliceCarData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabObjectGeometryData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ObjectGeometryData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PolicePatrolRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PolicePatrolRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PoliceEmergencyRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_PoliceEmergencyRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CrimeProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PoliceStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PoliceStation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AccidentSiteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_AccidentSite_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InvolvedInAccidentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InvolvedInAccident_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurveData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_Curve_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ConnectionLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ConnectionLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_BlockerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Blocker_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_SimulationFrameIndex = m_SimulationSystem.frameIndex,
			m_PolicePatrolRequestArchetype = m_PolicePatrolRequestArchetype,
			m_PoliceEmergencyRequestArchetype = m_PoliceEmergencyRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_MovingToParkedAircraftRemoveTypes = m_MovingToParkedAircraftRemoveTypes,
			m_MovingToParkedAddTypes = m_MovingToParkedAddTypes,
			m_RandomSeed = RandomSeed.Next(),
			m_ObjectSearchTree = m_ObjectSearchSystem.GetStaticSearchTree(readOnly: true, out dependencies),
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter(),
			m_ActionQueue = actionQueue.AsParallelWriter()
		};
		PoliceActionJob jobData2 = new PoliceActionJob
		{
			m_SimulationFrame = m_SimulationSystem.frameIndex,
			m_CrimeProducerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_CrimeProducer_RW_ComponentLookup, ref base.CheckedStateRef),
			m_AccidentSiteData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_AccidentSite_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ActionQueue = actionQueue
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_VehicleQuery, JobHandle.CombineDependencies(base.Dependency, dependencies));
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData2, jobHandle);
		actionQueue.Dispose(jobHandle2);
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
	public PoliceAircraftAISystem()
	{
	}
}
