#define UNITY_ASSERTIONS
using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Citizens;
using Game.City;
using Game.Common;
using Game.Economy;
using Game.Events;
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
public class EmergencyShelterAISystem : GameSystemBase
{
	private struct EmergencyShelterAction
	{
		public Entity m_Entity;

		public bool m_Disabled;

		public static EmergencyShelterAction SetDisabled(Entity vehicle, bool disabled)
		{
			return new EmergencyShelterAction
			{
				m_Entity = vehicle,
				m_Disabled = disabled
			};
		}
	}

	[BurstCompile]
	private struct EmergencyShelterTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabRefType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ResourceConsumer> m_ResourceConsumerType;

		public ComponentTypeHandle<Game.Buildings.EmergencyShelter> m_EmergencyShelterType;

		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		public ComponentTypeHandle<ServiceUsage> m_ServiceUsageType;

		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		public BufferTypeHandle<Occupant> m_OccupantType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public ComponentLookup<EvacuationRequest> m_EvacuationRequestData;

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
		public ComponentLookup<EmergencyShelterData> m_PrefabEmergencyShelterData;

		[ReadOnly]
		public ComponentLookup<PublicTransportVehicleData> m_PrefabPublicTransportVehicleData;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> m_TravelPurposeData;

		[ReadOnly]
		public ComponentLookup<Worker> m_WorkerData;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> m_StudentData;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> m_HouseholdMemberData;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> m_TouristHouseholdData;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> m_HomelessHouseholdData;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> m_PropertyRenterData;

		[ReadOnly]
		public ComponentLookup<InDanger> m_InDangerData;

		[ReadOnly]
		public TransportVehicleSelectData m_TransportVehicleSelectData;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_EvacuationRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingCarAddTypes;

		[ReadOnly]
		public float m_DangerLevelExitProbability;

		[ReadOnly]
		public float m_InoperableExitProbability;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<EmergencyShelterAction>.ParallelWriter m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<PrefabRef> nativeArray3 = chunk.GetNativeArray(ref m_PrefabRefType);
			NativeArray<Game.Buildings.EmergencyShelter> nativeArray4 = chunk.GetNativeArray(ref m_EmergencyShelterType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			NativeArray<Game.Buildings.ResourceConsumer> nativeArray5 = chunk.GetNativeArray(ref m_ResourceConsumerType);
			NativeArray<ServiceUsage> nativeArray6 = chunk.GetNativeArray(ref m_ServiceUsageType);
			BufferAccessor<InstalledUpgrade> bufferAccessor2 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<OwnedVehicle> bufferAccessor3 = chunk.GetBufferAccessor(ref m_OwnedVehicleType);
			BufferAccessor<ServiceDispatch> bufferAccessor4 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			BufferAccessor<Occupant> bufferAccessor5 = chunk.GetBufferAccessor(ref m_OccupantType);
			Span<float> span = stackalloc float[32];
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < nativeArray.Length; i++)
			{
				Entity entity = nativeArray[i];
				Transform transform = nativeArray2[i];
				PrefabRef prefabRef = nativeArray3[i];
				Game.Buildings.EmergencyShelter emergencyShelter = nativeArray4[i];
				DynamicBuffer<OwnedVehicle> vehicles = bufferAccessor3[i];
				DynamicBuffer<ServiceDispatch> dispatches = bufferAccessor4[i];
				DynamicBuffer<Occupant> occupants = bufferAccessor5[i];
				EmergencyShelterData data = default(EmergencyShelterData);
				if (m_PrefabEmergencyShelterData.HasComponent(prefabRef.m_Prefab))
				{
					data = m_PrefabEmergencyShelterData[prefabRef.m_Prefab];
				}
				if (bufferAccessor2.Length != 0)
				{
					UpgradeUtils.CombineStats(ref data, bufferAccessor2[i], ref m_PrefabRefData, ref m_PrefabEmergencyShelterData);
				}
				if (bufferAccessor.Length != 0)
				{
					BuildingUtils.GetEfficiencyFactors(bufferAccessor[i], span);
				}
				else
				{
					span.Fill(1f);
				}
				float immediateEfficiency = BuildingUtils.GetImmediateEfficiency(bufferAccessor, i);
				byte resourceAvailability = ((nativeArray5.Length != 0) ? nativeArray5[i].m_ResourceAvailability : byte.MaxValue);
				Tick(unfilteredChunkIndex, entity, transform, ref random, ref emergencyShelter, out var serviceUsage, data, vehicles, dispatches, occupants, span, immediateEfficiency, resourceAvailability);
				nativeArray4[i] = emergencyShelter;
				if (nativeArray6.Length != 0)
				{
					nativeArray6[i] = new ServiceUsage
					{
						m_Usage = serviceUsage
					};
				}
				if (bufferAccessor.Length != 0)
				{
					BuildingUtils.SetEfficiencyFactors(bufferAccessor[i], span);
				}
			}
		}

		private void Tick(int jobIndex, Entity entity, Transform transform, ref Unity.Mathematics.Random random, ref Game.Buildings.EmergencyShelter emergencyShelter, out float serviceUsage, EmergencyShelterData prefabEmergencyShelterData, DynamicBuffer<OwnedVehicle> vehicles, DynamicBuffer<ServiceDispatch> dispatches, DynamicBuffer<Occupant> occupants, Span<float> efficiencyFactors, float immediateEfficiency, byte resourceAvailability)
		{
			float num = ((resourceAvailability > 0) ? 1f : 0f);
			efficiencyFactors[17] = num;
			float efficiency = BuildingUtils.GetEfficiency(efficiencyFactors);
			bool flag = (double)efficiency > 0.001;
			for (int num2 = occupants.Length - 1; num2 >= 0; num2--)
			{
				if (m_PrefabRefData.HasComponent(occupants[num2]))
				{
					if (m_TravelPurposeData.TryGetComponent(occupants[num2], out var componentData) && componentData.m_Purpose == Purpose.InEmergencyShelter)
					{
						if (flag)
						{
							if (!IsCitizenInDanger(occupants[num2]) && random.NextFloat() < m_DangerLevelExitProbability)
							{
								m_CommandBuffer.RemoveComponent<TravelPurpose>(jobIndex, occupants[num2]);
							}
						}
						else if (random.NextFloat() < m_InoperableExitProbability)
						{
							m_CommandBuffer.RemoveComponent<TravelPurpose>(jobIndex, occupants[num2]);
						}
					}
				}
				else
				{
					occupants.RemoveAtSwapBack(num2);
				}
			}
			int availableSpace = (flag ? (prefabEmergencyShelterData.m_ShelterCapacity - occupants.Length) : 0);
			int vehicleCapacity = BuildingUtils.GetVehicleCapacity(math.min(efficiency, immediateEfficiency), prefabEmergencyShelterData.m_VehicleCapacity);
			int num3 = BuildingUtils.GetVehicleCapacity(immediateEfficiency, prefabEmergencyShelterData.m_VehicleCapacity);
			int availableVehicles = vehicleCapacity;
			serviceUsage = (flag ? ((float)occupants.Length / math.max(1f, prefabEmergencyShelterData.m_ShelterCapacity)) : 0f);
			StackList<Entity> parkedVehicles = stackalloc Entity[vehicles.Length];
			for (int i = 0; i < vehicles.Length; i++)
			{
				Entity vehicle = vehicles[i].m_Vehicle;
				if (!m_PublicTransportData.TryGetComponent(vehicle, out var componentData2))
				{
					continue;
				}
				if (m_ParkedCarData.TryGetComponent(vehicle, out var componentData3))
				{
					if (!m_EntityLookup.Exists(componentData3.m_Lane))
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
				bool flag2 = --num3 < 0;
				if ((componentData2.m_State & PublicTransportFlags.Disabled) != 0 != flag2)
				{
					m_ActionQueue.Enqueue(EmergencyShelterAction.SetDisabled(vehicle, flag2));
				}
			}
			int num4 = 0;
			while (num4 < dispatches.Length)
			{
				Entity request = dispatches[num4].m_Request;
				if (m_EvacuationRequestData.HasComponent(request))
				{
					SpawnVehicle(jobIndex, ref random, entity, request, transform, ref emergencyShelter, ref availableVehicles, ref availableSpace, ref parkedVehicles);
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
			while (parkedVehicles.Length > math.max(0, prefabEmergencyShelterData.m_VehicleCapacity + availableVehicles - vehicleCapacity))
			{
				int index = random.NextInt(parkedVehicles.Length);
				m_CommandBuffer.AddComponent<Deleted>(jobIndex, parkedVehicles[index]);
				parkedVehicles.RemoveAtSwapBack(index);
			}
			for (int j = 0; j < parkedVehicles.Length; j++)
			{
				Entity entity2 = parkedVehicles[j];
				Game.Vehicles.PublicTransport publicTransport = m_PublicTransportData[entity2];
				bool flag3 = availableVehicles <= 0 || availableSpace <= 0;
				if ((publicTransport.m_State & PublicTransportFlags.Disabled) != 0 != flag3)
				{
					m_ActionQueue.Enqueue(EmergencyShelterAction.SetDisabled(entity2, flag3));
				}
			}
			if (availableVehicles > 0)
			{
				emergencyShelter.m_Flags |= EmergencyShelterFlags.HasAvailableVehicles;
				RequestTargetIfNeeded(jobIndex, entity, ref emergencyShelter, availableVehicles);
			}
			else
			{
				emergencyShelter.m_Flags &= ~EmergencyShelterFlags.HasAvailableVehicles;
			}
			if (availableSpace > 0)
			{
				emergencyShelter.m_Flags |= EmergencyShelterFlags.HasShelterSpace;
			}
			else
			{
				emergencyShelter.m_Flags &= ~EmergencyShelterFlags.HasShelterSpace;
			}
		}

		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Buildings.EmergencyShelter emergencyShelter, int availableVehicles)
		{
			if (!m_ServiceRequestData.HasComponent(emergencyShelter.m_TargetRequest))
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_EvacuationRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
				m_CommandBuffer.SetComponent(jobIndex, e, new EvacuationRequest(entity, availableVehicles));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(4u));
			}
		}

		private void SpawnVehicle(int jobIndex, ref Unity.Mathematics.Random random, Entity entity, Entity request, Transform transform, ref Game.Buildings.EmergencyShelter emergencyShelter, ref int availableVehicles, ref int availableSpace, ref StackList<Entity> parkedVehicles)
		{
			if (!m_EvacuationRequestData.TryGetComponent(request, out var componentData) || !m_EntityLookup.Exists(componentData.m_Target) || availableVehicles <= 0 || availableSpace <= 0)
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
				entity2 = m_TransportVehicleSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, transform, entity, default(NativeList<VehicleModel>), TransportType.Bus, EnergyTypes.FuelAndElectricity, SizeClass.Large, PublicTransportPurpose.Evacuation, Resource.NoResource, ref passengerCapacity, ref cargoCapacity, parked: false);
				if (entity2 == Entity.Null)
				{
					return;
				}
				m_CommandBuffer.AddComponent(jobIndex, entity2, new Owner(entity));
			}
			availableVehicles--;
			availableSpace -= passengerCapacity.y;
			Game.Vehicles.PublicTransport component = default(Game.Vehicles.PublicTransport);
			component.m_State |= PublicTransportFlags.Evacuating;
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
			if (m_ServiceRequestData.HasComponent(emergencyShelter.m_TargetRequest))
			{
				e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(emergencyShelter.m_TargetRequest, Entity.Null, completed: true));
			}
		}

		private bool IsCitizenInDanger(Entity citizen)
		{
			if (m_HouseholdMemberData.TryGetComponent(citizen, out var componentData))
			{
				if (m_PropertyRenterData.TryGetComponent(componentData.m_Household, out var componentData2) && IsInDanger(componentData2.m_Property))
				{
					return true;
				}
				if (m_TouristHouseholdData.TryGetComponent(citizen, out var componentData3) && IsBuildingOrCompanyInDanger(componentData3.m_Hotel))
				{
					return true;
				}
				if (m_HomelessHouseholdData.TryGetComponent(citizen, out var componentData4) && IsInDanger(componentData4.m_TempHome))
				{
					return true;
				}
			}
			if (m_WorkerData.TryGetComponent(citizen, out var componentData5) && IsBuildingOrCompanyInDanger(componentData5.m_Workplace))
			{
				return true;
			}
			if (m_StudentData.TryGetComponent(citizen, out var componentData6) && IsInDanger(componentData6.m_School))
			{
				return true;
			}
			return false;
		}

		private bool IsBuildingOrCompanyInDanger(Entity entity)
		{
			if (IsInDanger(entity))
			{
				return true;
			}
			if (m_PropertyRenterData.TryGetComponent(entity, out var componentData) && IsInDanger(componentData.m_Property))
			{
				return true;
			}
			return false;
		}

		private bool IsInDanger(Entity entity)
		{
			return m_InDangerData.HasComponent(entity);
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct EmergencyShelterActionJob : IJob
	{
		public ComponentLookup<Game.Vehicles.PublicTransport> m_PublicTransportData;

		public NativeQueue<EmergencyShelterAction> m_ActionQueue;

		public void Execute()
		{
			EmergencyShelterAction item;
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
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ResourceConsumer> __Game_Buildings_ResourceConsumer_RO_ComponentTypeHandle;

		public ComponentTypeHandle<Game.Buildings.EmergencyShelter> __Game_Buildings_EmergencyShelter_RW_ComponentTypeHandle;

		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RW_BufferTypeHandle;

		public ComponentTypeHandle<ServiceUsage> __Game_Buildings_ServiceUsage_RW_ComponentTypeHandle;

		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;

		public BufferTypeHandle<Occupant> __Game_Buildings_Occupant_RW_BufferTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public ComponentLookup<EvacuationRequest> __Game_Simulation_EvacuationRequest_RO_ComponentLookup;

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
		public ComponentLookup<EmergencyShelterData> __Game_Prefabs_EmergencyShelterData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PublicTransportVehicleData> __Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<TravelPurpose> __Game_Citizens_TravelPurpose_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Worker> __Game_Citizens_Worker_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Citizens.Student> __Game_Citizens_Student_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HouseholdMember> __Game_Citizens_HouseholdMember_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TouristHousehold> __Game_Citizens_TouristHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HomelessHousehold> __Game_Citizens_HomelessHousehold_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PropertyRenter> __Game_Buildings_PropertyRenter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<InDanger> __Game_Events_InDanger_RO_ComponentLookup;

		public ComponentLookup<Game.Vehicles.PublicTransport> __Game_Vehicles_PublicTransport_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>(isReadOnly: true);
			__Game_Buildings_ResourceConsumer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.ResourceConsumer>(isReadOnly: true);
			__Game_Buildings_EmergencyShelter_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.EmergencyShelter>();
			__Game_Buildings_Efficiency_RW_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>();
			__Game_Buildings_ServiceUsage_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceUsage>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__Game_Buildings_Occupant_RW_BufferTypeHandle = state.GetBufferTypeHandle<Occupant>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Simulation_EvacuationRequest_RO_ComponentLookup = state.GetComponentLookup<EvacuationRequest>(isReadOnly: true);
			__Game_Simulation_ServiceRequest_RO_ComponentLookup = state.GetComponentLookup<ServiceRequest>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_EmergencyShelterData_RO_ComponentLookup = state.GetComponentLookup<EmergencyShelterData>(isReadOnly: true);
			__Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup = state.GetComponentLookup<PublicTransportVehicleData>(isReadOnly: true);
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Citizens_TravelPurpose_RO_ComponentLookup = state.GetComponentLookup<TravelPurpose>(isReadOnly: true);
			__Game_Citizens_Worker_RO_ComponentLookup = state.GetComponentLookup<Worker>(isReadOnly: true);
			__Game_Citizens_Student_RO_ComponentLookup = state.GetComponentLookup<Game.Citizens.Student>(isReadOnly: true);
			__Game_Citizens_HouseholdMember_RO_ComponentLookup = state.GetComponentLookup<HouseholdMember>(isReadOnly: true);
			__Game_Citizens_TouristHousehold_RO_ComponentLookup = state.GetComponentLookup<TouristHousehold>(isReadOnly: true);
			__Game_Citizens_HomelessHousehold_RO_ComponentLookup = state.GetComponentLookup<HomelessHousehold>(isReadOnly: true);
			__Game_Buildings_PropertyRenter_RO_ComponentLookup = state.GetComponentLookup<PropertyRenter>(isReadOnly: true);
			__Game_Events_InDanger_RO_ComponentLookup = state.GetComponentLookup<InDanger>(isReadOnly: true);
			__Game_Vehicles_PublicTransport_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.PublicTransport>();
		}
	}

	private EntityQuery m_BuildingQuery;

	private EntityQuery m_VehiclePrefabQuery;

	private EntityArchetype m_EvacuationRequestArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private ComponentTypeSet m_ParkedToMovingRemoveTypes;

	private ComponentTypeSet m_ParkedToMovingCarAddTypes;

	private EndFrameBarrier m_EndFrameBarrier;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private TransportVehicleSelectData m_TransportVehicleSelectData;

	private TypeHandle __TypeHandle;

	private EntityQuery __query_1553762682_0;

	private EntityQuery __query_1553762682_1;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 240;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_TransportVehicleSelectData = new TransportVehicleSelectData(this);
		m_BuildingQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.EmergencyShelter>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_VehiclePrefabQuery = GetEntityQuery(TransportVehicleSelectData.GetEntityQueryDesc());
		m_EvacuationRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<EvacuationRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_HandleRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<HandleRequest>(), ComponentType.ReadWrite<Game.Common.Event>());
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
		RequireForUpdate<DisasterConfigurationData>();
		RequireForUpdate<Game.City.DangerLevel>();
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_TransportVehicleSelectData.PreUpdate(this, m_CityConfigurationSystem, m_VehiclePrefabQuery, Allocator.TempJob, out var jobHandle);
		float dangerLevel = __query_1553762682_0.GetSingleton<Game.City.DangerLevel>().m_DangerLevel;
		DisasterConfigurationData singleton = __query_1553762682_1.GetSingleton<DisasterConfigurationData>();
		NativeQueue<EmergencyShelterAction> actionQueue = new NativeQueue<EmergencyShelterAction>(Allocator.TempJob);
		EmergencyShelterTickJob jobData = new EmergencyShelterTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabRefType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_OwnedVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ResourceConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ResourceConsumer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EmergencyShelterType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_EmergencyShelter_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceUsageType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ServiceUsage_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_OccupantType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Occupant_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_EvacuationRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_EvacuationRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PublicTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_PublicTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabEmergencyShelterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_EmergencyShelterData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPublicTransportVehicleData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PublicTransportVehicleData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_TravelPurposeData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TravelPurpose_RO_ComponentLookup, ref base.CheckedStateRef),
			m_WorkerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Worker_RO_ComponentLookup, ref base.CheckedStateRef),
			m_StudentData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_Student_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HouseholdMemberData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HouseholdMember_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TouristHouseholdData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_TouristHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HomelessHouseholdData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HomelessHousehold_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PropertyRenterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_PropertyRenter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_InDangerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Events_InDanger_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransportVehicleSelectData = m_TransportVehicleSelectData,
			m_RandomSeed = RandomSeed.Next(),
			m_EvacuationRequestArchetype = m_EvacuationRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_ParkedToMovingRemoveTypes = m_ParkedToMovingRemoveTypes,
			m_ParkedToMovingCarAddTypes = m_ParkedToMovingCarAddTypes,
			m_DangerLevelExitProbability = singleton.m_EmergencyShelterDangerLevelExitProbability.Evaluate(dangerLevel),
			m_InoperableExitProbability = singleton.m_InoperableEmergencyShelterExitProbability,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_ActionQueue = actionQueue.AsParallelWriter()
		};
		EmergencyShelterActionJob jobData2 = new EmergencyShelterActionJob
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
		EntityQueryBuilder entityQueryBuilder = new EntityQueryBuilder(Allocator.Temp);
		EntityQueryBuilder entityQueryBuilder2 = entityQueryBuilder.WithAll<Game.City.DangerLevel>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1553762682_0 = entityQueryBuilder2.Build(ref state);
		entityQueryBuilder.Reset();
		entityQueryBuilder2 = entityQueryBuilder.WithAll<DisasterConfigurationData>();
		entityQueryBuilder2 = entityQueryBuilder2.WithOptions(EntityQueryOptions.IncludeSystems);
		__query_1553762682_1 = entityQueryBuilder2.Build(ref state);
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
	public EmergencyShelterAISystem()
	{
	}
}
