#define UNITY_ASSERTIONS
using System;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Buildings;
using Game.Citizens;
using Game.City;
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
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Scripting;

namespace Game.Simulation;

[CompilerGenerated]
public class HospitalAISystem : GameSystemBase
{
	private struct HospitalAction
	{
		public Entity m_Entity;

		public bool m_Disabled;

		public static HospitalAction SetDisabled(Entity vehicle, bool disabled)
		{
			return new HospitalAction
			{
				m_Entity = vehicle,
				m_Disabled = disabled
			};
		}
	}

	[BurstCompile]
	private struct HospitalTickJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<PrefabRef> m_PrefabType;

		[ReadOnly]
		public BufferTypeHandle<OwnedVehicle> m_OwnedVehicleType;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> m_InstalledUpgradeType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ResourceConsumer> m_ResourceConsumerType;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> m_OutsideConnectionType;

		public ComponentTypeHandle<Game.Buildings.Hospital> m_HospitalType;

		public BufferTypeHandle<ServiceDispatch> m_ServiceDispatchType;

		public BufferTypeHandle<Patient> m_PatientType;

		public ComponentTypeHandle<ServiceUsage> m_ServiceUsageType;

		public BufferTypeHandle<Efficiency> m_EfficiencyType;

		[ReadOnly]
		public EntityStorageInfoLookup m_EntityLookup;

		[ReadOnly]
		public BufferLookup<PathElement> m_PathElements;

		[ReadOnly]
		public ComponentLookup<HospitalData> m_PrefabHospitalData;

		[ReadOnly]
		public ComponentLookup<PrefabRef> m_PrefabRefData;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> m_PrefabSpawnLocationData;

		[ReadOnly]
		public ComponentLookup<HealthcareRequest> m_HealthcareRequestData;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> m_ServiceRequestData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> m_SpawnLocationData;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> m_TransformData;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Ambulance> m_AmbulanceData;

		[ReadOnly]
		public ComponentLookup<Helicopter> m_HelicopterData;

		[ReadOnly]
		public ComponentLookup<ParkedCar> m_ParkedCarData;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> m_ParkingLaneData;

		[ReadOnly]
		public ComponentLookup<PathInformation> m_PathInformationData;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> m_CurrentBuildingData;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> m_CurrentTransportData;

		[ReadOnly]
		public ComponentLookup<HealthProblem> m_HealthProblemData;

		[ReadOnly]
		public BufferLookup<CityModifier> m_CityModifiers;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[ReadOnly]
		public EntityArchetype m_HealthcareRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_HandleRequestArchetype;

		[ReadOnly]
		public EntityArchetype m_ResetTripArchetype;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingRemoveTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingCarAddTypes;

		[ReadOnly]
		public ComponentTypeSet m_ParkedToMovingAircraftAddTypes;

		[ReadOnly]
		public HealthcareVehicleSelectData m_HealthcareVehicleSelectData;

		[ReadOnly]
		public HealthcareParameterData m_HealthcareParameterData;

		[ReadOnly]
		public Entity m_City;

		public EntityCommandBuffer.ParallelWriter m_CommandBuffer;

		public NativeQueue<HospitalAction>.ParallelWriter m_ActionQueue;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<PrefabRef> nativeArray2 = chunk.GetNativeArray(ref m_PrefabType);
			NativeArray<Game.Buildings.Hospital> nativeArray3 = chunk.GetNativeArray(ref m_HospitalType);
			BufferAccessor<Efficiency> bufferAccessor = chunk.GetBufferAccessor(ref m_EfficiencyType);
			NativeArray<Game.Buildings.ResourceConsumer> nativeArray4 = chunk.GetNativeArray(ref m_ResourceConsumerType);
			BufferAccessor<OwnedVehicle> bufferAccessor2 = chunk.GetBufferAccessor(ref m_OwnedVehicleType);
			BufferAccessor<InstalledUpgrade> bufferAccessor3 = chunk.GetBufferAccessor(ref m_InstalledUpgradeType);
			BufferAccessor<Patient> bufferAccessor4 = chunk.GetBufferAccessor(ref m_PatientType);
			BufferAccessor<ServiceDispatch> bufferAccessor5 = chunk.GetBufferAccessor(ref m_ServiceDispatchType);
			NativeArray<ServiceUsage> nativeArray5 = chunk.GetNativeArray(ref m_ServiceUsageType);
			bool outside = chunk.Has(ref m_OutsideConnectionType);
			Span<float> span = stackalloc float[32];
			DynamicBuffer<CityModifier> cityModifiers = m_CityModifiers[m_City];
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			for (int i = 0; i < chunk.Count; i++)
			{
				Entity entity = nativeArray[i];
				PrefabRef prefabRef = nativeArray2[i];
				ref Game.Buildings.Hospital hospital = ref nativeArray3.ElementAt(i);
				DynamicBuffer<OwnedVehicle> vehicles = bufferAccessor2[i];
				DynamicBuffer<ServiceDispatch> dispatches = bufferAccessor5[i];
				byte resourceAvailability = ((nativeArray4.Length != 0) ? nativeArray4[i].m_ResourceAvailability : byte.MaxValue);
				DynamicBuffer<Patient> patients = default(DynamicBuffer<Patient>);
				if (bufferAccessor4.Length != 0)
				{
					patients = bufferAccessor4[i];
				}
				m_PrefabHospitalData.TryGetComponent(prefabRef.m_Prefab, out var componentData);
				if (bufferAccessor3.Length != 0)
				{
					UpgradeUtils.CombineStats(ref componentData, bufferAccessor3[i], ref m_PrefabRefData, ref m_PrefabHospitalData);
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
				Tick(unfilteredChunkIndex, entity, ref random, ref hospital, out var usage, componentData, vehicles, patients, dispatches, span, immediateEfficiency, resourceAvailability, cityModifiers, outside);
				if (bufferAccessor.Length != 0)
				{
					BuildingUtils.SetEfficiencyFactors(bufferAccessor[i], span);
				}
				if (nativeArray5.Length != 0)
				{
					nativeArray5[i] = usage;
				}
			}
		}

		private void Tick(int jobIndex, Entity entity, ref Unity.Mathematics.Random random, ref Game.Buildings.Hospital hospital, out ServiceUsage usage, HospitalData prefabHospitalData, DynamicBuffer<OwnedVehicle> vehicles, DynamicBuffer<Patient> patients, DynamicBuffer<ServiceDispatch> dispatches, Span<float> efficiencyFactors, float immediateEfficiency, byte resourceAvailability, DynamicBuffer<CityModifier> cityModifiers, bool outside)
		{
			bool flag = false;
			bool flag2 = false;
			float num = ((resourceAvailability > 0) ? 1f : (1f - m_HealthcareParameterData.m_NoResourceTreatmentPenalty));
			if (!outside)
			{
				efficiencyFactors[17] = num;
				float value = 100f;
				CityUtils.ApplyModifier(ref value, cityModifiers, CityModifierType.HospitalEfficiency);
				efficiencyFactors[26] = value / 100f;
			}
			float efficiency = BuildingUtils.GetEfficiency(efficiencyFactors);
			if (hospital.m_TreatmentBonus != 0 && math.abs(efficiency - 0f) > float.Epsilon)
			{
				flag = prefabHospitalData.m_TreatDiseases;
				flag2 = prefabHospitalData.m_TreatInjuries;
			}
			int vehicleCapacity = BuildingUtils.GetVehicleCapacity(math.min(efficiency, immediateEfficiency), prefabHospitalData.m_AmbulanceCapacity);
			int vehicleCapacity2 = BuildingUtils.GetVehicleCapacity(math.min(efficiency, immediateEfficiency), prefabHospitalData.m_MedicalHelicopterCapacity);
			int num2 = BuildingUtils.GetVehicleCapacity(immediateEfficiency, prefabHospitalData.m_AmbulanceCapacity);
			int num3 = BuildingUtils.GetVehicleCapacity(immediateEfficiency, prefabHospitalData.m_MedicalHelicopterCapacity);
			int availableVehicles = vehicleCapacity;
			int availableVehicles2 = vehicleCapacity2;
			StackList<Entity> parkedVehicles = stackalloc Entity[vehicles.Length];
			StackList<Entity> parkedVehicles2 = stackalloc Entity[vehicles.Length];
			hospital.m_TreatmentBonus = (byte)math.min(255, Mathf.RoundToInt(efficiency * num * (float)prefabHospitalData.m_TreatmentBonus));
			hospital.m_MinHealth = (byte)prefabHospitalData.m_HealthRange.x;
			hospital.m_MaxHealth = (byte)prefabHospitalData.m_HealthRange.y;
			for (int i = 0; i < vehicles.Length; i++)
			{
				Entity vehicle = vehicles[i].m_Vehicle;
				if (!m_AmbulanceData.TryGetComponent(vehicle, out var componentData))
				{
					continue;
				}
				bool flag3 = m_HelicopterData.HasComponent(vehicle);
				if (m_ParkedCarData.TryGetComponent(vehicle, out var componentData2))
				{
					if (!m_EntityLookup.Exists(componentData2.m_Lane))
					{
						m_CommandBuffer.AddComponent<Deleted>(jobIndex, vehicle);
					}
					else if (flag3)
					{
						parkedVehicles2.AddNoResize(vehicle);
					}
					else
					{
						parkedVehicles.AddNoResize(vehicle);
					}
					continue;
				}
				bool flag4;
				if (flag3)
				{
					availableVehicles2--;
					flag4 = --num3 < 0;
				}
				else
				{
					availableVehicles--;
					flag4 = --num2 < 0;
				}
				if ((componentData.m_State & AmbulanceFlags.Disabled) != 0 != flag4)
				{
					m_ActionQueue.Enqueue(HospitalAction.SetDisabled(vehicle, flag4));
				}
			}
			int num4 = 0;
			while (num4 < dispatches.Length)
			{
				Entity request = dispatches[num4].m_Request;
				if (m_HealthcareRequestData.TryGetComponent(request, out var componentData3))
				{
					if (componentData3.m_Type == HealthcareRequestType.Ambulance)
					{
						RoadTypes roadTypes = CheckPathType(request);
						switch (roadTypes)
						{
						case RoadTypes.Car:
							SpawnVehicle(jobIndex, ref random, entity, request, roadTypes, flag, ref hospital, ref availableVehicles, ref parkedVehicles);
							break;
						case RoadTypes.Helicopter:
							SpawnVehicle(jobIndex, ref random, entity, request, roadTypes, flag, ref hospital, ref availableVehicles2, ref parkedVehicles2);
							break;
						}
						dispatches.RemoveAt(num4);
					}
					else
					{
						num4++;
					}
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
			while (parkedVehicles.Length > math.max(0, prefabHospitalData.m_AmbulanceCapacity + availableVehicles - vehicleCapacity))
			{
				int index = random.NextInt(parkedVehicles.Length);
				m_CommandBuffer.AddComponent<Deleted>(jobIndex, parkedVehicles[index]);
				parkedVehicles.RemoveAtSwapBack(index);
			}
			while (parkedVehicles2.Length > math.max(0, prefabHospitalData.m_MedicalHelicopterCapacity + availableVehicles2 - vehicleCapacity2))
			{
				int index2 = random.NextInt(parkedVehicles2.Length);
				m_CommandBuffer.AddComponent<Deleted>(jobIndex, parkedVehicles2[index2]);
				parkedVehicles2.RemoveAtSwapBack(index2);
			}
			for (int j = 0; j < parkedVehicles.Length; j++)
			{
				Entity entity2 = parkedVehicles[j];
				Game.Vehicles.Ambulance ambulance = m_AmbulanceData[entity2];
				bool flag5 = availableVehicles <= 0;
				if ((ambulance.m_State & AmbulanceFlags.Disabled) != 0 != flag5)
				{
					m_ActionQueue.Enqueue(HospitalAction.SetDisabled(entity2, flag5));
				}
			}
			for (int k = 0; k < parkedVehicles2.Length; k++)
			{
				Entity entity3 = parkedVehicles2[k];
				Game.Vehicles.Ambulance ambulance2 = m_AmbulanceData[entity3];
				bool flag6 = availableVehicles2 <= 0;
				if ((ambulance2.m_State & AmbulanceFlags.Disabled) != 0 != flag6)
				{
					m_ActionQueue.Enqueue(HospitalAction.SetDisabled(entity3, flag6));
				}
			}
			hospital.m_Flags &= ~(HospitalFlags.HasAvailableAmbulances | HospitalFlags.HasAvailableMedicalHelicopters | HospitalFlags.CanCureDisease | HospitalFlags.HasRoomForPatients | HospitalFlags.CanProcessCorpses | HospitalFlags.CanCureInjury);
			if (availableVehicles != 0)
			{
				hospital.m_Flags |= HospitalFlags.HasAvailableAmbulances;
			}
			if (availableVehicles2 != 0)
			{
				hospital.m_Flags |= HospitalFlags.HasAvailableMedicalHelicopters;
			}
			if (flag)
			{
				hospital.m_Flags |= HospitalFlags.CanCureDisease;
			}
			if (flag2)
			{
				hospital.m_Flags |= HospitalFlags.CanCureInjury;
			}
			if (prefabHospitalData.m_PatientCapacity > 0)
			{
				hospital.m_Flags |= HospitalFlags.CanProcessCorpses;
			}
			if (patients.IsCreated)
			{
				int num5 = patients.Length - 1;
				while (patients.Length > 0 && num5 >= 0)
				{
					Entity patient = patients[num5].m_Patient;
					if (!m_HealthProblemData.HasComponent(patient))
					{
						patients.RemoveAt(num5);
						num5--;
						continue;
					}
					HealthProblem healthProblem = m_HealthProblemData[patient];
					if ((healthProblem.m_Flags & HealthProblemFlags.Dead) != HealthProblemFlags.None)
					{
						m_CommandBuffer.AddComponent(jobIndex, patient, default(Deleted));
						patients.RemoveAt(num5);
						num5--;
						continue;
					}
					if ((!flag && (healthProblem.m_Flags & HealthProblemFlags.Sick) != HealthProblemFlags.None) || (!flag2 && (healthProblem.m_Flags & HealthProblemFlags.Injured) != HealthProblemFlags.None))
					{
						m_CommandBuffer.RemoveComponent<CurrentBuilding>(jobIndex, patient);
						if (m_CurrentTransportData.HasComponent(patient))
						{
							Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_ResetTripArchetype);
							m_CommandBuffer.SetComponent(jobIndex, e, new ResetTrip
							{
								m_Creature = m_CurrentTransportData[patient].m_CurrentTransport,
								m_Target = Entity.Null
							});
						}
						patients.RemoveAt(num5);
					}
					num5--;
				}
				if (patients.Length < prefabHospitalData.m_PatientCapacity)
				{
					hospital.m_Flags |= HospitalFlags.HasRoomForPatients;
				}
				usage.m_Usage = (float)patients.Length / math.max(1f, prefabHospitalData.m_PatientCapacity);
			}
			else
			{
				usage.m_Usage = 0f;
			}
			if ((hospital.m_Flags & (HospitalFlags.HasAvailableAmbulances | HospitalFlags.HasAvailableMedicalHelicopters)) != 0)
			{
				RequestTargetIfNeeded(jobIndex, entity, ref hospital);
			}
		}

		private void RequestTargetIfNeeded(int jobIndex, Entity entity, ref Game.Buildings.Hospital hospital)
		{
			if (!m_ServiceRequestData.HasComponent(hospital.m_TargetRequest))
			{
				Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HealthcareRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new ServiceRequest(reversed: true));
				m_CommandBuffer.SetComponent(jobIndex, e, new HealthcareRequest(entity, HealthcareRequestType.Ambulance));
				m_CommandBuffer.SetComponent(jobIndex, e, new RequestGroup(16u));
			}
		}

		private void SpawnVehicle(int jobIndex, ref Unity.Mathematics.Random random, Entity entity, Entity request, RoadTypes roadType, bool canCurePatients, ref Game.Buildings.Hospital hospital, ref int availableVehicles, ref StackList<Entity> parkedVehicles)
		{
			if (availableVehicles <= 0 || !m_HealthcareRequestData.TryGetComponent(request, out var componentData))
			{
				return;
			}
			Entity citizen = componentData.m_Citizen;
			Entity entity2 = Entity.Null;
			CurrentBuilding componentData3;
			if (m_CurrentTransportData.TryGetComponent(citizen, out var componentData2))
			{
				entity2 = componentData2.m_CurrentTransport;
			}
			else if (m_CurrentBuildingData.TryGetComponent(citizen, out componentData3))
			{
				entity2 = componentData3.m_CurrentBuilding;
			}
			if (!m_EntityLookup.Exists(entity2))
			{
				return;
			}
			Entity entity3 = Entity.Null;
			if (m_PathInformationData.TryGetComponent(request, out var componentData4) && componentData4.m_Origin != entity)
			{
				if (!CollectionUtils.RemoveValueSwapBack(ref parkedVehicles, componentData4.m_Origin))
				{
					return;
				}
				ParkedCar parkedCar = m_ParkedCarData[componentData4.m_Origin];
				entity3 = componentData4.m_Origin;
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
				entity3 = m_HealthcareVehicleSelectData.CreateVehicle(m_CommandBuffer, jobIndex, ref random, m_TransformData[entity], entity, Entity.Null, componentData.m_Type, roadType, parked: false);
				if (entity3 == Entity.Null)
				{
					return;
				}
				m_CommandBuffer.AddComponent(jobIndex, entity3, new Owner(entity));
			}
			availableVehicles--;
			AmbulanceFlags ambulanceFlags = AmbulanceFlags.Dispatched;
			ambulanceFlags |= AmbulanceFlags.AnyHospital;
			m_CommandBuffer.SetComponent(jobIndex, entity3, new Game.Vehicles.Ambulance(citizen, entity2, ambulanceFlags));
			m_CommandBuffer.SetComponent(jobIndex, entity3, new Target(entity2));
			m_CommandBuffer.SetBuffer<ServiceDispatch>(jobIndex, entity3).Add(new ServiceDispatch(request));
			Entity e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
			m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(request, entity3, completed: false));
			if (m_PathElements.TryGetBuffer(request, out var bufferData) && bufferData.Length != 0)
			{
				DynamicBuffer<PathElement> targetElements = m_CommandBuffer.SetBuffer<PathElement>(jobIndex, entity3);
				PathUtils.CopyPath(bufferData, default(PathOwner), 0, targetElements);
				m_CommandBuffer.SetComponent(jobIndex, entity3, new PathOwner(PathFlags.Updated));
				m_CommandBuffer.SetComponent(jobIndex, entity3, componentData4);
			}
			if (m_ServiceRequestData.HasComponent(hospital.m_TargetRequest))
			{
				e = m_CommandBuffer.CreateEntity(jobIndex, m_HandleRequestArchetype);
				m_CommandBuffer.SetComponent(jobIndex, e, new HandleRequest(hospital.m_TargetRequest, Entity.Null, completed: true));
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
	private struct HospitalActionJob : IJob
	{
		public ComponentLookup<Game.Vehicles.Ambulance> m_AmbulanceData;

		public NativeQueue<HospitalAction> m_ActionQueue;

		public void Execute()
		{
			HospitalAction item;
			while (m_ActionQueue.TryDequeue(out item))
			{
				if (m_AmbulanceData.TryGetComponent(item.m_Entity, out var componentData))
				{
					if (item.m_Disabled)
					{
						componentData.m_State |= AmbulanceFlags.Disabled;
					}
					else
					{
						componentData.m_State &= ~AmbulanceFlags.Disabled;
					}
					m_AmbulanceData[item.m_Entity] = componentData;
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
		public BufferTypeHandle<OwnedVehicle> __Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InstalledUpgrade> __Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Buildings.ResourceConsumer> __Game_Buildings_ResourceConsumer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Game.Objects.OutsideConnection> __Game_Objects_OutsideConnection_RO_ComponentTypeHandle;

		public BufferTypeHandle<Patient> __Game_Buildings_Patient_RW_BufferTypeHandle;

		public ComponentTypeHandle<Game.Buildings.Hospital> __Game_Buildings_Hospital_RW_ComponentTypeHandle;

		public BufferTypeHandle<ServiceDispatch> __Game_Simulation_ServiceDispatch_RW_BufferTypeHandle;

		public BufferTypeHandle<Efficiency> __Game_Buildings_Efficiency_RW_BufferTypeHandle;

		public ComponentTypeHandle<ServiceUsage> __Game_Buildings_ServiceUsage_RW_ComponentTypeHandle;

		[ReadOnly]
		public EntityStorageInfoLookup __EntityStorageInfoLookup;

		[ReadOnly]
		public BufferLookup<PathElement> __Game_Pathfind_PathElement_RO_BufferLookup;

		[ReadOnly]
		public ComponentLookup<HospitalData> __Game_Prefabs_HospitalData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrefabRef> __Game_Prefabs_PrefabRef_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SpawnLocationData> __Game_Prefabs_SpawnLocationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthcareRequest> __Game_Simulation_HealthcareRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ServiceRequest> __Game_Simulation_ServiceRequest_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.Transform> __Game_Objects_Transform_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Objects.SpawnLocation> __Game_Objects_SpawnLocation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Vehicles.Ambulance> __Game_Vehicles_Ambulance_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Helicopter> __Game_Vehicles_Helicopter_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkedCar> __Game_Vehicles_ParkedCar_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Net.ParkingLane> __Game_Net_ParkingLane_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PathInformation> __Game_Pathfind_PathInformation_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentBuilding> __Game_Citizens_CurrentBuilding_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CurrentTransport> __Game_Citizens_CurrentTransport_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HealthProblem> __Game_Citizens_HealthProblem_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<CityModifier> __Game_City_CityModifier_RO_BufferLookup;

		public ComponentLookup<Game.Vehicles.Ambulance> __Game_Vehicles_Ambulance_RW_ComponentLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabRef>(isReadOnly: true);
			__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle = state.GetBufferTypeHandle<OwnedVehicle>(isReadOnly: true);
			__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle = state.GetBufferTypeHandle<InstalledUpgrade>(isReadOnly: true);
			__Game_Buildings_ResourceConsumer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.ResourceConsumer>(isReadOnly: true);
			__Game_Objects_OutsideConnection_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Objects.OutsideConnection>(isReadOnly: true);
			__Game_Buildings_Patient_RW_BufferTypeHandle = state.GetBufferTypeHandle<Patient>();
			__Game_Buildings_Hospital_RW_ComponentTypeHandle = state.GetComponentTypeHandle<Game.Buildings.Hospital>();
			__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle = state.GetBufferTypeHandle<ServiceDispatch>();
			__Game_Buildings_Efficiency_RW_BufferTypeHandle = state.GetBufferTypeHandle<Efficiency>();
			__Game_Buildings_ServiceUsage_RW_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceUsage>();
			__EntityStorageInfoLookup = state.GetEntityStorageInfoLookup();
			__Game_Pathfind_PathElement_RO_BufferLookup = state.GetBufferLookup<PathElement>(isReadOnly: true);
			__Game_Prefabs_HospitalData_RO_ComponentLookup = state.GetComponentLookup<HospitalData>(isReadOnly: true);
			__Game_Prefabs_PrefabRef_RO_ComponentLookup = state.GetComponentLookup<PrefabRef>(isReadOnly: true);
			__Game_Prefabs_SpawnLocationData_RO_ComponentLookup = state.GetComponentLookup<SpawnLocationData>(isReadOnly: true);
			__Game_Simulation_HealthcareRequest_RO_ComponentLookup = state.GetComponentLookup<HealthcareRequest>(isReadOnly: true);
			__Game_Simulation_ServiceRequest_RO_ComponentLookup = state.GetComponentLookup<ServiceRequest>(isReadOnly: true);
			__Game_Objects_Transform_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.Transform>(isReadOnly: true);
			__Game_Objects_SpawnLocation_RO_ComponentLookup = state.GetComponentLookup<Game.Objects.SpawnLocation>(isReadOnly: true);
			__Game_Vehicles_Ambulance_RO_ComponentLookup = state.GetComponentLookup<Game.Vehicles.Ambulance>(isReadOnly: true);
			__Game_Vehicles_Helicopter_RO_ComponentLookup = state.GetComponentLookup<Helicopter>(isReadOnly: true);
			__Game_Vehicles_ParkedCar_RO_ComponentLookup = state.GetComponentLookup<ParkedCar>(isReadOnly: true);
			__Game_Net_ParkingLane_RO_ComponentLookup = state.GetComponentLookup<Game.Net.ParkingLane>(isReadOnly: true);
			__Game_Pathfind_PathInformation_RO_ComponentLookup = state.GetComponentLookup<PathInformation>(isReadOnly: true);
			__Game_Citizens_CurrentBuilding_RO_ComponentLookup = state.GetComponentLookup<CurrentBuilding>(isReadOnly: true);
			__Game_Citizens_CurrentTransport_RO_ComponentLookup = state.GetComponentLookup<CurrentTransport>(isReadOnly: true);
			__Game_Citizens_HealthProblem_RO_ComponentLookup = state.GetComponentLookup<HealthProblem>(isReadOnly: true);
			__Game_City_CityModifier_RO_BufferLookup = state.GetBufferLookup<CityModifier>(isReadOnly: true);
			__Game_Vehicles_Ambulance_RW_ComponentLookup = state.GetComponentLookup<Game.Vehicles.Ambulance>();
		}
	}

	private EndFrameBarrier m_EndFrameBarrier;

	private CitySystem m_CitySystem;

	private EntityQuery m_HospitalQuery;

	private EntityQuery m_HealthcareVehiclePrefabQuery;

	private EntityQuery m_HealthcareParameterQuery;

	private EntityArchetype m_HealthcareRequestArchetype;

	private EntityArchetype m_HandleRequestArchetype;

	private EntityArchetype m_ResetTripArchetype;

	private ComponentTypeSet m_ParkedToMovingRemoveTypes;

	private ComponentTypeSet m_ParkedToMovingCarAddTypes;

	private ComponentTypeSet m_ParkedToMovingAircraftAddTypes;

	private CityConfigurationSystem m_CityConfigurationSystem;

	private HealthcareVehicleSelectData m_HealthcareVehicleSelectData;

	private TypeHandle __TypeHandle;

	public override int GetUpdateInterval(SystemUpdatePhase phase)
	{
		return 256;
	}

	public override int GetUpdateOffset(SystemUpdatePhase phase)
	{
		return 16;
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_EndFrameBarrier = base.World.GetOrCreateSystemManaged<EndFrameBarrier>();
		m_CitySystem = base.World.GetOrCreateSystemManaged<CitySystem>();
		m_CityConfigurationSystem = base.World.GetOrCreateSystemManaged<CityConfigurationSystem>();
		m_HealthcareVehicleSelectData = new HealthcareVehicleSelectData(this);
		m_HospitalQuery = GetEntityQuery(ComponentType.ReadOnly<Game.Buildings.Hospital>(), ComponentType.ReadOnly<ServiceDispatch>(), ComponentType.ReadOnly<PrefabRef>(), ComponentType.Exclude<Temp>(), ComponentType.Exclude<Deleted>());
		m_HealthcareRequestArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<ServiceRequest>(), ComponentType.ReadWrite<HealthcareRequest>(), ComponentType.ReadWrite<RequestGroup>());
		m_ResetTripArchetype = base.EntityManager.CreateArchetype(ComponentType.ReadWrite<Game.Common.Event>(), ComponentType.ReadWrite<ResetTrip>());
		m_HealthcareVehiclePrefabQuery = GetEntityQuery(HealthcareVehicleSelectData.GetEntityQueryDesc());
		m_HealthcareParameterQuery = GetEntityQuery(ComponentType.ReadOnly<HealthcareParameterData>());
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
		RequireForUpdate(m_HospitalQuery);
		Assert.IsTrue(condition: true);
	}

	[Preserve]
	protected override void OnUpdate()
	{
		m_HealthcareVehicleSelectData.PreUpdate(this, m_CityConfigurationSystem, m_HealthcareVehiclePrefabQuery, Allocator.TempJob, out var jobHandle);
		NativeQueue<HospitalAction> actionQueue = new NativeQueue<HospitalAction>(Allocator.TempJob);
		HospitalTickJob jobData = new HospitalTickJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_PrefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OwnedVehicleType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Vehicles_OwnedVehicle_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InstalledUpgradeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_InstalledUpgrade_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_ResourceConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ResourceConsumer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_OutsideConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_OutsideConnection_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PatientType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Patient_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_HospitalType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Hospital_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceDispatchType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Simulation_ServiceDispatch_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_EfficiencyType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Buildings_Efficiency_RW_BufferTypeHandle, ref base.CheckedStateRef),
			m_ServiceUsageType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ServiceUsage_RW_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EntityLookup = InternalCompilerInterface.GetEntityStorageInfoLookup(ref __TypeHandle.__EntityStorageInfoLookup, ref base.CheckedStateRef),
			m_PathElements = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Pathfind_PathElement_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabHospitalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HospitalData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabRefData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrefabRef_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SpawnLocationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthcareRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_HealthcareRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceRequestData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Simulation_ServiceRequest_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransformData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentLookup, ref base.CheckedStateRef),
			m_SpawnLocationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Objects_SpawnLocation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_AmbulanceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Ambulance_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HelicopterData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Helicopter_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkedCarData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_ParkedCar_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ParkingLaneData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Net_ParkingLane_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PathInformationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Pathfind_PathInformation_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentBuildingData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentBuilding_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CurrentTransportData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_CurrentTransport_RO_ComponentLookup, ref base.CheckedStateRef),
			m_HealthProblemData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Citizens_HealthProblem_RO_ComponentLookup, ref base.CheckedStateRef),
			m_CityModifiers = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_City_CityModifier_RO_BufferLookup, ref base.CheckedStateRef),
			m_RandomSeed = RandomSeed.Next(),
			m_HealthcareRequestArchetype = m_HealthcareRequestArchetype,
			m_HandleRequestArchetype = m_HandleRequestArchetype,
			m_ResetTripArchetype = m_ResetTripArchetype,
			m_ParkedToMovingRemoveTypes = m_ParkedToMovingRemoveTypes,
			m_ParkedToMovingCarAddTypes = m_ParkedToMovingCarAddTypes,
			m_ParkedToMovingAircraftAddTypes = m_ParkedToMovingAircraftAddTypes,
			m_HealthcareVehicleSelectData = m_HealthcareVehicleSelectData,
			m_HealthcareParameterData = m_HealthcareParameterQuery.GetSingleton<HealthcareParameterData>(),
			m_City = m_CitySystem.City,
			m_CommandBuffer = m_EndFrameBarrier.CreateCommandBuffer().AsParallelWriter(),
			m_ActionQueue = actionQueue.AsParallelWriter()
		};
		HospitalActionJob jobData2 = new HospitalActionJob
		{
			m_AmbulanceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Vehicles_Ambulance_RW_ComponentLookup, ref base.CheckedStateRef),
			m_ActionQueue = actionQueue
		};
		JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData, m_HospitalQuery, JobHandle.CombineDependencies(base.Dependency, jobHandle));
		JobHandle jobHandle3 = IJobExtensions.Schedule(jobData2, jobHandle2);
		actionQueue.Dispose(jobHandle3);
		m_HealthcareVehicleSelectData.PostUpdate(jobHandle2);
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
	public HospitalAISystem()
	{
	}
}
