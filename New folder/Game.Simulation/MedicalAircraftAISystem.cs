using System.Runtime.CompilerServices;
using Game.Areas;
using Game.Buildings;
using Game.Citizens;
using Game.Common;
using Game.Creatures;
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
public class MedicalAircraftAISystem : GameSystemBase
{
	[BurstCompile]
	private struct MedicalAircraftTickJob : IJobChunk
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

		[ReadOnly]
		public ComponentTypeHandle<Stopped> m_StoppedType;

		public ComponentTypeHandle<Game.Vehicles.Ambulance> m_AmbulanceType;

		public ComponentTypeHandle<Aircraft> m_AircraftType;

		public ComponentTypeHandle<AircraftCurrentLane> m_CurrentLaneType;

		public ComponentTypeHandle<Target> m_TargetType;

		public ComponentTypeHandle<PathOwner> m_PathOwnerType;

		public BufferTypeHandle<AircraftNavigationLane> m_AircraftNavigationLaneType;

		public BufferTypeHandle<Passenger> m_PassengerType;

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
		public ComponentLookup<HealthcareRequest> m_HealthcareRequestData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Hospital> m_HospitalData;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> m_CurrentBuildingData;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> m_CurrentTransportData;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblemData;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> m_TravelPurposeData;

		[ReadOnly]
		public ComponentLookup<Citizen> m_CitizenData;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> m_CurrentVehicleData;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> m_CurrentDistrictData;

		[ReadOnly]
		public BufferLookup<LayoutElement> m_LayoutElements;

		[NativeDisableParallelForRestriction]
		public ComponentLookup<Blocker> m_BlockerData;

		[NativeDisableParallelForRestriction]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedAircraftRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_MovingToParkedAddTypes;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<SetupQueueItem>.ParallelWriter m_PathfindQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Owner> nativeArray2 = chunk.GetNativeArray(ref m_OwnerType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<PathInformation> nativeArray4 = chunk.GetNativeArray(ref m_PathInformationType);
			NativeArray<AircraftCurrentLane> nativeArray5 = chunk.GetNativeArray(ref m_CurrentLaneType);
			NativeArray<Game.Vehicles.Ambulance> nativeArray6 = chunk.GetNativeArray(ref m_AmbulanceType);
			NativeArray<Aircraft> nativeArray7 = chunk.GetNativeArray(ref m_AircraftType);
			NativeArray<Target> nativeArray8 = chunk.GetNativeArray(ref m_TargetType);
			NativeArray<PathOwner> nativeArray9 = chunk.GetNativeArray(ref m_PathOwnerType);
			BufferAccessor<AircraftNavigationLane> bufferAccessor = chunk.GetBufferAccessor(ref m_AircraftNavigationLaneType);
			BufferAccessor<Passenger> bufferAccessor2 = chunk.GetBufferAccessor(ref m_PassengerType);
			BufferAccessor<ServiceDispatch> bufferAccessor3 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			bool isStopped = chunk.Has(ref m_StoppedType);
			bool isUnspawned = chunk.Has(ref m_UnspawnedType);
			Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Owner owner = nativeArray2[i];
				PrefabRef prefabRef = nativeArray3[i];
				PathInformation pathInformation = nativeArray4[i];
				Game.Vehicles.Ambulance ambulance = nativeArray6[i];
				Aircraft aircraft = nativeArray7[i];
				AircraftCurrentLane currentLane = nativeArray5[i];
				PathOwner pathOwner = nativeArray9[i];
				Target target = nativeArray8[i];
				DynamicBuffer<AircraftNavigationLane> navigationLanes = bufferAccessor[i];
				DynamicBuffer<Passenger> passengers = bufferAccessor2[i];
				DynamicBuffer<ServiceDispatch> serviceDispatches = bufferAccessor3[i];
				VehicleUtils.CheckUnspawned(unfilteredChunkIndex, entity, currentLane, isUnspawned, m_CommandBuffer);
				Tick(unfilteredChunkIndex, entity, owner, prefabRef, pathInformation, navigationLanes, passengers, serviceDispatches, isStopped, ref random, ref ambulance, ref aircraft, ref currentLane, ref pathOwner, ref target);
				nativeArray6[i] = ambulance;
				nativeArray7[i] = aircraft;
				nativeArray5[i] = currentLane;
				nativeArray9[i] = pathOwner;
				nativeArray8[i] = target;
			}
		}

		private void Tick(int jobIndex, Entity entity, Owner owner, PrefabRef prefabRef, PathInformation pathInformation, DynamicBuffer<AircraftNavigationLane> navigationLanes, DynamicBuffer<Passenger> passengers, DynamicBuffer<ServiceDispatch> serviceDispatches, bool isStopped, ref Random random, ref Game.Vehicles.Ambulance ambulance, ref Aircraft aircraft, ref AircraftCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
		{
			CheckServiceDispatches(entity, serviceDispatches, ref ambulance);
			if (VehicleUtils.ResetUpdatedPath(ref pathOwner))
			{
				ResetPath(jobIndex, entity, pathInformation, passengers, serviceDispatches, ref ambulance, ref aircraft, ref currentLane, ref target);
			}
			if (VehicleUtils.IsStuck(pathOwner))
			{
				Blocker blocker = m_BlockerData[entity];
				bool num = m_ParkedCarData.HasComponent(blocker.m_Blocker);
				if (num)
				{
					Entity entity2 = blocker.m_Blocker;
					if (m_ControllerData.TryGetComponent(entity2, out var componentData))
					{
						entity2 = componentData.m_Controller;
					}
					m_LayoutElements.TryGetBuffer(entity2, out var bufferData);
					VehicleUtils.DeleteVehicle(m_CommandBuffer, jobIndex, entity2, bufferData);
				}
				if (num || blocker.m_Blocker == Entity.Null)
				{
					pathOwner.m_State &= ~PathFlags.Stuck;
					m_BlockerData[entity] = default(Blocker);
				}
			}
			if (!m_EntityLookup.Exists(target.m_Target) || VehicleUtils.PathfindFailed(pathOwner))
			{
				if (VehicleUtils.IsStuck(pathOwner) || (ambulance.m_State & AmbulanceFlags.Returning) != 0)
				{
					m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
				}
				else
				{
					ReturnToDepot(owner, serviceDispatches, ref ambulance, ref aircraft, ref pathOwner, ref target);
				}
				return;
			}
			if (VehicleUtils.PathEndReached(currentLane) || (ambulance.m_State & (AmbulanceFlags.AtTarget | AmbulanceFlags.Disembarking)) != 0)
			{
				if ((ambulance.m_State & AmbulanceFlags.Returning) != 0)
				{
					if (UnloadPatients(passengers, ref ambulance))
					{
						if (VehicleUtils.ParkingSpaceReached(currentLane, pathOwner))
						{
							ParkAircraft(jobIndex, entity, owner, ref aircraft, ref ambulance, ref currentLane);
						}
						else
						{
							m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
						}
					}
					return;
				}
				if ((ambulance.m_State & AmbulanceFlags.Transporting) != 0)
				{
					if (!UnloadPatients(passengers, ref ambulance))
					{
						return;
					}
					if (!SelectNextDispatch(jobIndex, entity, navigationLanes, serviceDispatches, ref ambulance, ref aircraft, ref currentLane, ref pathOwner, ref target))
					{
						if (target.m_Target == owner.m_Owner)
						{
							if (VehicleUtils.ParkingSpaceReached(currentLane, pathOwner))
							{
								ParkAircraft(jobIndex, entity, owner, ref aircraft, ref ambulance, ref currentLane);
							}
							else
							{
								m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
							}
							return;
						}
						ReturnToDepot(owner, serviceDispatches, ref ambulance, ref aircraft, ref pathOwner, ref target);
					}
				}
				else if (LoadPatients(jobIndex, entity, passengers, serviceDispatches, isStopped, ref random, ref ambulance, ref target))
				{
					if ((ambulance.m_State & AmbulanceFlags.Transporting) != 0)
					{
						TransportToHospital(owner, serviceDispatches, ref ambulance, ref aircraft, ref pathOwner, ref target);
					}
					else if (!SelectNextDispatch(jobIndex, entity, navigationLanes, serviceDispatches, ref ambulance, ref aircraft, ref currentLane, ref pathOwner, ref target))
					{
						if (target.m_Target == owner.m_Owner)
						{
							if (VehicleUtils.ParkingSpaceReached(currentLane, pathOwner))
							{
								ParkAircraft(jobIndex, entity, owner, ref aircraft, ref ambulance, ref currentLane);
							}
							else
							{
								m_CommandBuffer.AddComponent(jobIndex, entity, default(Deleted));
							}
							return;
						}
						ReturnToDepot(owner, serviceDispatches, ref ambulance, ref aircraft, ref pathOwner, ref target);
					}
				}
			}
			else
			{
				if ((ambulance.m_State & (AmbulanceFlags.Returning | AmbulanceFlags.Transporting | AmbulanceFlags.Disabled)) == AmbulanceFlags.Disabled)
				{
					ReturnToDepot(owner, serviceDispatches, ref ambulance, ref aircraft, ref pathOwner, ref target);
				}
				if (isStopped)
				{
					StartVehicle(jobIndex, entity);
				}
			}
			if ((ambulance.m_State & (AmbulanceFlags.Returning | AmbulanceFlags.Dispatched | AmbulanceFlags.Transporting)) == (AmbulanceFlags.Returning | AmbulanceFlags.Dispatched) && !SelectNextDispatch(jobIndex, entity, navigationLanes, serviceDispatches, ref ambulance, ref aircraft, ref currentLane, ref pathOwner, ref target))
			{
				serviceDispatches.Clear();
				ambulance.m_State &= ~AmbulanceFlags.Dispatched;
			}
			if ((ambulance.m_State & (AmbulanceFlags.AtTarget | AmbulanceFlags.Disembarking)) == 0)
			{
				if (VehicleUtils.RequireNewPath(pathOwner))
				{
					FindNewPath(entity, prefabRef, ref ambulance, ref aircraft, ref currentLane, ref pathOwner, ref target);
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

		private void ParkAircraft(int jobIndex, Entity entity, Owner owner, ref Aircraft aircraft, ref Game.Vehicles.Ambulance ambulance, ref AircraftCurrentLane currentLane)
		{
			aircraft.m_Flags &= ~(AircraftFlags.Emergency | AircraftFlags.IgnoreParkedVehicle);
			ambulance.m_State = (AmbulanceFlags)0u;
			if (m_HospitalData.TryGetComponent(owner.m_Owner, out var componentData) && (componentData.m_Flags & HospitalFlags.HasAvailableMedicalHelicopters) == 0)
			{
				ambulance.m_State |= AmbulanceFlags.Disabled;
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

		private void StopVehicle(int jobIndex, Entity entity)
		{
			m_CommandBuffer.RemoveComponent<Moving>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<TransformFrame>(jobIndex, entity);
			m_CommandBuffer.RemoveComponent<InterpolatedTransform>(jobIndex, entity);
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Stopped));
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Updated));
		}

		private void StartVehicle(int jobIndex, Entity entity)
		{
			m_CommandBuffer.RemoveComponent<Stopped>(jobIndex, entity);
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Moving));
			m_CommandBuffer.AddBuffer<TransformFrame>(jobIndex, entity);
			m_CommandBuffer.AddComponent(jobIndex, entity, default(InterpolatedTransform));
			m_CommandBuffer.AddComponent(jobIndex, entity, default(Updated));
		}

		private bool LoadPatients(int jobIndex, Entity entity, DynamicBuffer<Passenger> passengers, DynamicBuffer<ServiceDispatch> serviceDispatches, bool isStopped, ref Random random, ref Game.Vehicles.Ambulance ambulance, ref Target target)
		{
			ambulance.m_State |= AmbulanceFlags.AtTarget;
			bool flag = false;
			if (serviceDispatches.Length == 0 || (ambulance.m_State & AmbulanceFlags.Dispatched) == 0)
			{
				ambulance.m_TargetPatient = Entity.Null;
				return true;
			}
			if (!m_HealthProblemData.HasComponent(ambulance.m_TargetPatient))
			{
				ambulance.m_TargetPatient = Entity.Null;
				return true;
			}
			HealthProblem healthProblem = m_HealthProblemData[ambulance.m_TargetPatient];
			if (healthProblem.m_HealthcareRequest != serviceDispatches[0].m_Request || (healthProblem.m_Flags & HealthProblemFlags.RequireTransport) == 0)
			{
				ambulance.m_TargetPatient = Entity.Null;
				return true;
			}
			if (m_CurrentBuildingData.TryGetComponent(ambulance.m_TargetPatient, out var componentData) && componentData.m_CurrentBuilding != Entity.Null)
			{
				flag |= componentData.m_CurrentBuilding == target.m_Target;
			}
			if (m_CurrentTransportData.TryGetComponent(ambulance.m_TargetPatient, out var componentData2) && componentData2.m_CurrentTransport != Entity.Null)
			{
				flag |= componentData2.m_CurrentTransport == target.m_Target;
				if (!flag && ((m_TravelPurposeData.TryGetComponent(ambulance.m_TargetPatient, out var componentData3) && componentData3.m_Purpose == Purpose.Hospital) || componentData3.m_Purpose == Purpose.Deathcare))
				{
					flag = true;
				}
				if (flag && m_CurrentVehicleData.TryGetComponent(componentData2.m_CurrentTransport, out var componentData4))
				{
					if (componentData4.m_Vehicle != entity)
					{
						flag = false;
					}
					else if ((componentData4.m_Flags & CreatureVehicleFlags.Ready) != 0)
					{
						for (int i = 0; i < passengers.Length; i++)
						{
							if (passengers[i].m_Passenger == componentData2.m_CurrentTransport)
							{
								ambulance.m_State |= AmbulanceFlags.Transporting;
								if (m_CitizenData.TryGetComponent(ambulance.m_TargetPatient, out var componentData5) && random.NextInt(100) >= componentData5.m_Health)
								{
									ambulance.m_State |= AmbulanceFlags.Critical;
								}
								Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
								m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(healthProblem.m_HealthcareRequest, entity, completed: true));
								return true;
							}
						}
					}
				}
			}
			if (flag)
			{
				if (!isStopped)
				{
					StopVehicle(jobIndex, entity);
				}
				return false;
			}
			ambulance.m_TargetPatient = Entity.Null;
			return true;
		}

		private bool UnloadPatients(DynamicBuffer<Passenger> passengers, ref Game.Vehicles.Ambulance ambulance)
		{
			if (passengers.Length > 0)
			{
				ambulance.m_State |= AmbulanceFlags.Disembarking;
				return false;
			}
			passengers.Clear();
			ambulance.m_State &= ~(AmbulanceFlags.Transporting | AmbulanceFlags.Disembarking | AmbulanceFlags.Critical);
			ambulance.m_TargetPatient = Entity.Null;
			return true;
		}

		private void FindNewPath(Entity vehicleEntity, PrefabRef prefabRef, ref Game.Vehicles.Ambulance ambulance, ref Aircraft aircraft, ref AircraftCurrentLane currentLane, ref PathOwner pathOwner, ref Target target)
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
			SetupQueueTarget destination = default(SetupQueueTarget);
			if ((ambulance.m_State & AmbulanceFlags.FindHospital) != 0)
			{
				destination.m_Entity = FindDistrict(ambulance.m_TargetLocation);
				destination.m_Type = SetupTargetType.Hospital;
				destination.m_Methods = PathMethod.Road;
				destination.m_RoadTypes = RoadTypes.Helicopter;
			}
			else if ((ambulance.m_State & AmbulanceFlags.Returning) != 0)
			{
				destination.m_Type = SetupTargetType.CurrentLocation;
				destination.m_Methods = PathMethod.Road;
				destination.m_RoadTypes = RoadTypes.Helicopter;
				destination.m_Entity = target.m_Target;
			}
			else
			{
				destination.m_Type = SetupTargetType.CurrentLocation;
				destination.m_Methods = PathMethod.Road | PathMethod.Flying;
				destination.m_RoadTypes = RoadTypes.Helicopter;
				destination.m_FlyingTypes = RoadTypes.Helicopter;
				destination.m_Entity = target.m_Target;
			}
			if ((ambulance.m_State & (AmbulanceFlags.Returning | AmbulanceFlags.Dispatched | AmbulanceFlags.Transporting)) == AmbulanceFlags.Dispatched || (ambulance.m_State & (AmbulanceFlags.Transporting | AmbulanceFlags.Critical)) == (AmbulanceFlags.Transporting | AmbulanceFlags.Critical))
			{
				parameters.m_Weights = new PathfindWeights(1f, 0f, 0f, 0f);
				parameters.m_IgnoredRules = RuleFlags.ForbidHeavyTraffic;
			}
			else
			{
				parameters.m_Weights = new PathfindWeights(1f, 1f, 1f, 1f);
			}
			if ((ambulance.m_State & (AmbulanceFlags.Returning | AmbulanceFlags.Dispatched | AmbulanceFlags.Transporting)) != AmbulanceFlags.Dispatched)
			{
				destination.m_RandomCost = 30f;
			}
			VehicleUtils.SetupPathfind(item: new SetupQueueItem(vehicleEntity, parameters, origin, destination), currentLane: ref currentLane, pathOwner: ref pathOwner, queue: m_PathfindQueue);
		}

		private Entity FindDistrict(Entity building)
		{
			if (m_CurrentDistrictData.HasComponent(building))
			{
				return m_CurrentDistrictData[building].m_District;
			}
			return Entity.Null;
		}

		private void CheckServiceDispatches(Entity vehicleEntity, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.Ambulance ambulance)
		{
			if ((ambulance.m_State & AmbulanceFlags.Transporting) != 0)
			{
				serviceDispatches.Clear();
				return;
			}
			if ((ambulance.m_State & AmbulanceFlags.Dispatched) != 0)
			{
				if (serviceDispatches.Length > 1)
				{
					serviceDispatches.RemoveRange(1, serviceDispatches.Length - 1);
				}
				return;
			}
			Entity entity = Entity.Null;
			for (int i = 0; i < serviceDispatches.Length; i++)
			{
				Entity request = serviceDispatches[i].m_Request;
				if (m_HealthcareRequestData.HasComponent(request))
				{
					HealthcareRequest healthcareRequest = m_HealthcareRequestData[request];
					if (m_CurrentTransportData.HasComponent(healthcareRequest.m_Citizen) || m_CurrentBuildingData.HasComponent(healthcareRequest.m_Citizen))
					{
						entity = request;
						break;
					}
				}
			}
			if (entity != Entity.Null)
			{
				serviceDispatches[0] = new ServiceDispatch(entity);
				if (serviceDispatches.Length > 1)
				{
					serviceDispatches.RemoveRange(1, serviceDispatches.Length - 1);
				}
				ambulance.m_State |= AmbulanceFlags.Dispatched;
			}
			else
			{
				serviceDispatches.Clear();
			}
		}

		private bool SelectNextDispatch(int jobIndex, Entity vehicleEntity, DynamicBuffer<AircraftNavigationLane> navigationLanes, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.Ambulance ambulance, ref Aircraft aircraft, ref AircraftCurrentLane currentLane, ref PathOwner pathOwner, ref Target targetData)
		{
			if ((ambulance.m_State & AmbulanceFlags.Returning) == 0 && (ambulance.m_State & AmbulanceFlags.Dispatched) != 0 && serviceDispatches.Length > 0)
			{
				serviceDispatches.RemoveAt(0);
				ambulance.m_State &= ~AmbulanceFlags.Dispatched;
			}
			while ((ambulance.m_State & AmbulanceFlags.Dispatched) != 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				Entity entity = Entity.Null;
				if (m_HealthcareRequestData.HasComponent(request))
				{
					entity = m_HealthcareRequestData[request].m_Citizen;
				}
				Entity entity2 = Entity.Null;
				if (m_CurrentTransportData.HasComponent(entity))
				{
					entity2 = m_CurrentTransportData[entity].m_CurrentTransport;
				}
				else if (m_CurrentBuildingData.HasComponent(entity))
				{
					entity2 = m_CurrentBuildingData[entity].m_CurrentBuilding;
				}
				if (!m_EntityLookup.Exists(entity2))
				{
					serviceDispatches.RemoveAt(0);
					ambulance.m_State &= ~AmbulanceFlags.Dispatched;
					continue;
				}
				aircraft.m_Flags &= ~AircraftFlags.IgnoreParkedVehicle;
				ambulance.m_TargetPatient = entity;
				ambulance.m_TargetLocation = entity2;
				ambulance.m_State &= ~(AmbulanceFlags.Returning | AmbulanceFlags.FindHospital | AmbulanceFlags.AtTarget);
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, vehicleEntity, completed: false, pathConsumed: true));
				if (m_PathElements.HasBuffer(request))
				{
					DynamicBuffer<PathElement> appendPath = m_PathElements[request];
					if (appendPath.Length != 0)
					{
						DynamicBuffer<PathElement> dynamicBuffer = m_PathElements[vehicleEntity];
						PathUtils.TrimPath(dynamicBuffer, ref pathOwner);
						float num = ambulance.m_PathElementTime * (float)dynamicBuffer.Length + m_PathInformationData[request].m_Duration;
						if (PathUtils.TryAppendPath(ref currentLane, navigationLanes, dynamicBuffer, appendPath))
						{
							ambulance.m_PathElementTime = num / (float)math.max(1, dynamicBuffer.Length);
							targetData.m_Target = entity2;
							VehicleUtils.ClearEndOfPath(ref currentLane, navigationLanes);
							aircraft.m_Flags |= AircraftFlags.Emergency;
							return true;
						}
					}
				}
				VehicleUtils.SetTarget(ref pathOwner, ref targetData, entity2);
				return true;
			}
			return false;
		}

		private void TransportToHospital(Owner owner, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.Ambulance ambulance, ref Aircraft aircraft, ref PathOwner pathOwner, ref Target target)
		{
			if ((ambulance.m_State & AmbulanceFlags.AnyHospital) != 0)
			{
				serviceDispatches.Clear();
				ambulance.m_State &= ~(AmbulanceFlags.Dispatched | AmbulanceFlags.AtTarget);
				ambulance.m_State |= AmbulanceFlags.FindHospital;
				VehicleUtils.SetTarget(ref pathOwner, ref target, Entity.Null);
			}
			else
			{
				ReturnToDepot(owner, serviceDispatches, ref ambulance, ref aircraft, ref pathOwner, ref target);
			}
		}

		private void ReturnToDepot(Owner owner, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.Ambulance ambulance, ref Aircraft aircraft, ref PathOwner pathOwner, ref Target target)
		{
			serviceDispatches.Clear();
			aircraft.m_Flags &= ~AircraftFlags.IgnoreParkedVehicle;
			ambulance.m_State &= ~(AmbulanceFlags.Dispatched | AmbulanceFlags.FindHospital | AmbulanceFlags.AtTarget);
			ambulance.m_State |= AmbulanceFlags.Returning;
			VehicleUtils.SetTarget(ref pathOwner, ref target, owner.m_Owner);
		}

		private void ResetPath(int jobIndex, Entity vehicleEntity, PathInformation pathInformation, DynamicBuffer<Passenger> passengers, DynamicBuffer<ServiceDispatch> serviceDispatches, ref Game.Vehicles.Ambulance ambulance, ref Aircraft aircraft, ref AircraftCurrentLane currentLane, ref Target target)
		{
			DynamicBuffer<PathElement> path = m_PathElements[vehicleEntity];
			PathUtils.ResetPath(ref currentLane, path);
			if ((ambulance.m_State & AmbulanceFlags.FindHospital) != 0)
			{
				target.m_Target = pathInformation.m_Destination;
				ambulance.m_State &= ~AmbulanceFlags.FindHospital;
				for (int i = 0; i < passengers.Length; i++)
				{
					Entity passenger = passengers[i].m_Passenger;
					if (m_CurrentVehicleData.HasComponent(passenger))
					{
						m_CommandBuffer.SetComponent(jobIndex, passenger, target);
					}
				}
			}
			if ((ambulance.m_State & AmbulanceFlags.Dispatched) != 0 && serviceDispatches.Length > 0)
			{
				Entity request = serviceDispatches[0].m_Request;
				if (m_HealthcareRequestData.HasComponent(request))
				{
					aircraft.m_Flags |= AircraftFlags.Emergency;
				}
				else
				{
					aircraft.m_Flags &= ~AircraftFlags.Emergency;
				}
			}
			else if ((ambulance.m_State & AmbulanceFlags.Critical) != 0)
			{
				aircraft.m_Flags |= AircraftFlags.Emergency;
			}
			else
			{
				aircraft.m_Flags &= ~AircraftFlags.Emergency;
			}
			ambulance.m_PathElementTime = pathInformation.m_Duration / (float)math.max(1, path.Length);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
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
		public ComponentTypeHandle<Stopped> __Game_Objects_Stopped_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Vehicles.Ambulance> __Game_Vehicles_Ambulance_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Aircraft> __Game_Vehicles_Aircraft_RW_ComponentTypeHandle;

		public ComponentTypeHandle<AircraftCurrentLane> __Game_Vehicles_AircraftCurrentLane_RW_ComponentTypeHandle;

		public ComponentTypeHandle<Target> __Game_Common_Target_RW_ComponentTypeHandle;

		public ComponentTypeHandle<PathOwner> __Game_Pathfind_PathOwner_RW_ComponentTypeHandle;

		public BufferTypeHandle<AircraftNavigationLane> __Game_Vehicles_AircraftNavigationLane_RW_BufferTypeHandle;

		public BufferTypeHandle<Passenger> __Game_Vehicles_Passenger_RW_BufferTypeHandle;

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
		public ComponentLookup<HealthcareRequest> __Game_Simulation_HealthcareRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Buildings.Hospital> __Game_Buildings_Hospital_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Citizen> __Game_Citizens_Citizen_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentVehicle> __Game_Creatures_CurrentVehicle_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentDistrict> __Game_Areas_CurrentDistrict_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<LayoutElement> __Game_Vehicles_LayoutElement_RO_BufferLookup;

		public ComponentLookup<Blocker> __Game_Vehicles_Blocker_RW_ComponentLookup;

		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Common_Owner_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Owner>(isReadOnly: true);
			__Game_Objects_Unspawned_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Unspawned>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PathInformation>(isReadOnly: true);
			__Game_Objects_Stopped_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Stopped>(isReadOnly: true);
			__Game_Vehicles_Ambulance_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Vehicles.Ambulance>();
			__Game_Vehicles_Aircraft_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Aircraft>();
			__Game_Vehicles_AircraftCurrentLane_RW_ComponentTypeHandle = state.GetComponentTypeHandle<AircraftCurrentLane>();
			__Game_Common_Target_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Target>();
			__Game_Pathfind_PathOwner_RW_ComponentTypeHandle = state.GetComponentTypeHandle<PathOwner>();
			__Game_Vehicles_AircraftNavigationLane_RW_BufferTypeHandle = state.GetBufferTypeHandle<AircraftNavigationLane>();
			__Game_Vehicles_Passenger_RW_BufferTypeHandle = state.GetBufferTypeHandle<Passenger>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Vehicles_Controller_RO_ComponentLookup = state.GetComponentLookup<Controller>(isReadOnly: true);
			__Game_Prefabs_HelicopterData_RO_ComponentLookup = state.GetComponentLookup<HelicopterData>(isReadOnly: true);
			__Game_Simulation_HealthcareRequest_RO_ComponentLookup = state.GetComponentLookup<HealthcareRequest>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Buildings_Hospital_RO_ComponentLookup = state.GetComponentLookup<Game.Buildings.Hospital>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_CurrentTransport_RO_ComponentLookup = state.GetComponentLookup<CurrentTransport>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
			__Game_Citizens_Citizen_RO_ComponentLookup = state.GetComponentLookup<Citizen>(isReadOnly: true);
			__Game_Creatures_CurrentVehicle_RO_ComponentLookup = state.GetComponentLookup<CurrentVehicle>(isReadOnly: true);
			__Game_Areas_CurrentDistrict_RO_ComponentLookup = state.GetComponentLookup<CurrentDistrict>(isReadOnly: true);
			__Game_Vehicles_LayoutElement_RO_BufferLookup = state.GetBufferLookup<LayoutElement>(isReadOnly: true);
			__Game_Vehicles_Blocker_RW_ComponentLookup = state.GetComponentLookup<Blocker>();
			__Game_Pathfind_PathElement_RW_BufferLookup = state.GetBufferLookup<PathElement>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private PathfindSetupSystem m_PathfindSetupSystem;

	private EntityQuery m_VehicleQuery;

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
		m_PathfindSetupSystem = base.World.GetOrCreateSystemManaged<PathfindSetupSystem>();
		m_VehicleQuery = GetEntityQuery(ComponentType.ReadWrite<Game.Vehicles.Ambulance>(), ComponentType.ReadWrite<AircraftCurrentLane>(), ComponentType.ReadOnly<Owner>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.ReadWrite<PathOwner>(), ComponentType.ReadWrite<Target>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<TripSource>(), ComponentType.Exclude<OutOfControl>());
		m_HandleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(), ComponentType.ReadWrite<Event>());
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
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(new MedicalAircraftTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_OwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Owner_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_UnspawnedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Unspawned_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathInformationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_StoppedType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Stopped_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AmbulanceType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Ambulance_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AircraftType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_Aircraft_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CurrentLaneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftCurrentLane_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TargetType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Common_Target_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PathOwnerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Pathfind_PathOwner_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AircraftNavigationLaneType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_AircraftNavigationLane_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_PassengerType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_Passenger_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ControllerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Controller_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabHelicopterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HelicopterData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthcareRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_HealthcareRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HospitalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_Hospital_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblemData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TravelPurposeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CitizenData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Citizen_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Creatures_CurrentVehicle_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentDistrictData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Areas_CurrentDistrict_RO_ComponentLookup, ref base.CheckedStateRef),
			m_LayoutElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Vehicles_LayoutElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_BlockerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Blocker_RW_ComponentLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RW_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_MovingToParkedAircraftRemoveTypes = m_MovingToParkedAircraftRemoveTypes,
			m_MovingToParkedAddTypes = m_MovingToParkedAddTypes,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_PathfindQueue = m_PathfindSetupSystem.GetQueue(this, 64).AsParallelWriter()
		}, m_VehicleQuery, base.Dependency);
		m_PathfindSetupSystem.AddQueueWriter(jobHandle);
		m_EndFrameBarrier.AddJobHandleForProducer(jobHandle);
		base.Dependency = jobHandle;
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
	public MedicalAircraftAISystem()
	{
	}
}
