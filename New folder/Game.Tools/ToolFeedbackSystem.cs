using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Entities;
using Colossal.Serialization.Entities;
using Game.Buildings;
using Game.City;
using Game.Common;
using Game.Net;
using Game.Notifications;
using Game.Objects;
using Game.Pathfind;
using Game.Prefabs;
using Game.Routes;
using Game.Serialization;
using Game.Simulation;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Tools;

[CompilerGenerated]
public class ToolFeedbackSystem : GameSystemBase, IPostDeserialize
{
	private struct RecentKey : IEquatable<RecentKey>
	{
		private Entity m_Entity;

		private int m_Type;

		public RecentKey(Entity entity, CoverageService coverageService)
		{
			m_Entity = entity;
			m_Type = (int)coverageService | 0x100;
		}

		public RecentKey(Entity entity, FeedbackType feedbackType)
		{
			m_Entity = entity;
			m_Type = (int)feedbackType | 0x200;
		}

		public RecentKey(Entity entity, LocalModifierType localModifierType)
		{
			m_Entity = entity;
			m_Type = (int)(localModifierType | (LocalModifierType)768);
		}

		public RecentKey(Entity entity, CityModifierType cityModifierType)
		{
			m_Entity = entity;
			m_Type = (int)(cityModifierType | (CityModifierType)1024);
		}

		public RecentKey(Entity entity, MaintenanceType maintenanceType)
		{
			m_Entity = entity;
			m_Type = (int)maintenanceType | 0x500;
		}

		public RecentKey(Entity entity, TransportType transportType)
		{
			m_Entity = entity;
			m_Type = (int)(transportType | (TransportType)1536);
		}

		public bool Equals(RecentKey other)
		{
			if (m_Entity == other.m_Entity)
			{
				return m_Type == other.m_Type;
			}
			return false;
		}

		public override int GetHashCode()
		{
			return m_Entity.GetHashCode() * 1792 + m_Type;
		}
	}

	private struct RecentValue
	{
		public uint m_UpdateFrame;

		public float m_FeedbackDelta;
	}

	private struct RecentUpdate
	{
		public RecentKey m_Key;

		public float m_Delta;
	}

	private enum FeedbackType : byte
	{
		GarbageVehicles,
		HospitalAmbulances,
		HospitalHelicopters,
		HospitalCapacity,
		DeathcareHearses,
		DeathcareCapacity,
		Electricity,
		Transformer,
		WaterCapacity,
		SewageCapacity,
		TransportDispatch,
		PublicTransport,
		CargoTransport,
		GroundPollution,
		AirPollution,
		NoisePollution,
		PostFacilityVehicles,
		PostFacilityCapacity,
		TelecomCoverage,
		ElementarySchoolCapacity,
		HighSchoolCapacity,
		CollegeCapacity,
		UniversityCapacity,
		ParkingSpaces,
		FireStationEngines,
		FireStationHelicopters,
		PoliceStationCars,
		PoliceStationHelicopters,
		PoliceStationCapacity,
		PrisonVehicles,
		PrisonCapacity,
		Attractiveness
	}

	[BurstCompile]
	private struct SetupCoverageSearchJob : IJob
	{
		[ReadOnly]
		public Entity m_Entity;

		[ReadOnly]
		public ComponentLookup<BackSide> m_BackSideData;

		[ReadOnly]
		public ComponentLookup<CoverageData> m_PrefabCoverageData;

		public CoverageAction m_Action;

		public PathfindTargetSeeker<PathfindTargetBuffer> m_TargetSeeker;

		public void Execute()
		{
			PrefabRef prefabRef = m_TargetSeeker.m_PrefabRef[m_Entity];
			if (!m_PrefabCoverageData.TryGetComponent(prefabRef.m_Prefab, out var componentData))
			{
				componentData.m_Range = 25000f;
			}
			if (m_TargetSeeker.m_Building.TryGetComponent(m_Entity, out var componentData2))
			{
				Transform transform = m_TargetSeeker.m_Transform[m_Entity];
				if (componentData2.m_RoadEdge != Entity.Null)
				{
					BuildingData buildingData = m_TargetSeeker.m_BuildingData[prefabRef.m_Prefab];
					float3 comparePosition = transform.m_Position;
					if (!m_TargetSeeker.m_Owner.TryGetComponent(componentData2.m_RoadEdge, out var componentData3) || componentData3.m_Owner != m_Entity)
					{
						comparePosition = BuildingUtils.CalculateFrontPosition(transform, buildingData.m_LotSize.y);
					}
					Unity.Mathematics.Random random = m_TargetSeeker.m_RandomSeed.GetRandom(m_Entity.Index);
					m_TargetSeeker.AddEdgeTargets(ref random, m_Entity, 0f, EdgeFlags.DefaultMask, componentData2.m_RoadEdge, comparePosition, 0f, allowLaneGroupSwitch: true, allowAccessRestriction: false);
				}
			}
			else
			{
				m_TargetSeeker.FindTargets(m_Entity, 0f);
			}
			if (m_BackSideData.TryGetComponent(m_Entity, out var componentData4))
			{
				Transform transform2 = m_TargetSeeker.m_Transform[m_Entity];
				if (componentData4.m_RoadEdge != Entity.Null)
				{
					BuildingData buildingData2 = m_TargetSeeker.m_BuildingData[prefabRef.m_Prefab];
					float3 comparePosition2 = transform2.m_Position;
					if (!m_TargetSeeker.m_Owner.TryGetComponent(componentData4.m_RoadEdge, out var componentData5) || componentData5.m_Owner != m_Entity)
					{
						comparePosition2 = BuildingUtils.CalculateFrontPosition(transform2, -buildingData2.m_LotSize.y);
					}
					Unity.Mathematics.Random random2 = m_TargetSeeker.m_RandomSeed.GetRandom(m_Entity.Index);
					m_TargetSeeker.AddEdgeTargets(ref random2, m_Entity, 0f, EdgeFlags.DefaultMask, componentData4.m_RoadEdge, comparePosition2, 0f, allowLaneGroupSwitch: true, allowAccessRestriction: false);
				}
			}
			m_Action.data.m_Parameters = new CoverageParameters
			{
				m_Methods = m_TargetSeeker.m_PathfindParameters.m_Methods,
				m_Range = componentData.m_Range
			};
		}
	}

	[BurstCompile]
	private struct FillCoverageMapJob : IJobParallelFor
	{
		[ReadOnly]
		public NativeArray<CoverageElement> m_CoverageElements;

		public NativeParallelHashMap<Entity, float2>.ParallelWriter m_CoverageMap;

		public void Execute(int index)
		{
			CoverageElement coverageElement = m_CoverageElements[index];
			m_CoverageMap.TryAdd(coverageElement.m_Edge, coverageElement.m_Cost);
		}
	}

	[BurstCompile]
	private struct TargetCheckJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<Transform> m_TransformType;

		[ReadOnly]
		public ComponentTypeHandle<Building> m_BuildingType;

		[ReadOnly]
		public ComponentTypeHandle<GarbageProducer> m_GarbageProducerType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityConsumer> m_ElectricityConsumerType;

		[ReadOnly]
		public ComponentTypeHandle<WaterConsumer> m_WaterConsumerType;

		[ReadOnly]
		public ComponentTypeHandle<MailProducer> m_MailProducerType;

		[ReadOnly]
		public ComponentTypeHandle<CrimeProducer> m_CrimeProducerType;

		[ReadOnly]
		public ComponentLookup<CoverageData> m_PrefabCoverageData;

		[ReadOnly]
		public ComponentLookup<GarbageFacilityData> m_PrefabGarbageFacilityData;

		[ReadOnly]
		public ComponentLookup<HospitalData> m_PrefabHospitalData;

		[ReadOnly]
		public ComponentLookup<DeathcareFacilityData> m_PrefabDeathcareFacilityData;

		[ReadOnly]
		public ComponentLookup<PowerPlantData> m_PrefabPowerPlantData;

		[ReadOnly]
		public ComponentLookup<WindPoweredData> m_PrefabWindPoweredData;

		[ReadOnly]
		public ComponentLookup<SolarPoweredData> m_PrefabSolarPoweredData;

		[ReadOnly]
		public ComponentLookup<Game.Prefabs.TransformerData> m_PrefabTransformerData;

		[ReadOnly]
		public ComponentLookup<WaterPumpingStationData> m_PrefabWaterPumpingStationData;

		[ReadOnly]
		public ComponentLookup<SewageOutletData> m_PrefabSewageOutletData;

		[ReadOnly]
		public ComponentLookup<WastewaterTreatmentPlantData> m_PrefabWastewaterTreatmentPlantData;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> m_PrefabTransportDepotData;

		[ReadOnly]
		public ComponentLookup<TransportStationData> m_PrefabTransportStationData;

		[ReadOnly]
		public ComponentLookup<PublicTransportStationData> m_PrefabPublicTransportStationData;

		[ReadOnly]
		public ComponentLookup<CargoTransportStationData> m_PrefabCargoTransportStationData;

		[ReadOnly]
		public ComponentLookup<TransportStopData> m_PrefabTransportStopData;

		[ReadOnly]
		public ComponentLookup<PostFacilityData> m_PrefabPostFacilityData;

		[ReadOnly]
		public ComponentLookup<TelecomFacilityData> m_PrefabTelecomFacilityData;

		[ReadOnly]
		public ComponentLookup<SchoolData> m_PrefabSchoolData;

		[ReadOnly]
		public ComponentLookup<ParkingFacilityData> m_PrefabParkingFacilityData;

		[ReadOnly]
		public ComponentLookup<MaintenanceDepotData> m_PrefabMaintenanceDepotData;

		[ReadOnly]
		public ComponentLookup<FireStationData> m_PrefabFireStationData;

		[ReadOnly]
		public ComponentLookup<PoliceStationData> m_PrefabPoliceStationData;

		[ReadOnly]
		public ComponentLookup<PrisonData> m_PrefabPrisonData;

		[ReadOnly]
		public ComponentLookup<PollutionData> m_PrefabPollutionData;

		[ReadOnly]
		public ComponentLookup<AttractionData> m_PrefabAttractionData;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> m_ServiceCoverages;

		[ReadOnly]
		public BufferLookup<LocalModifierData> m_PrefabLocalModifierDatas;

		[ReadOnly]
		public BufferLookup<CityModifierData> m_PrefabCityModifierDatas;

		[ReadOnly]
		public Feedback m_FeedbackData;

		[ReadOnly]
		public DynamicBuffer<ExtraFeedback> m_ExtraFeedbacks;

		[ReadOnly]
		public RandomSeed m_RandomSeed;

		[NativeDisableContainerSafetyRestriction]
		[ReadOnly]
		public NativeParallelHashMap<Entity, float2> m_CoverageMap;

		[ReadOnly]
		public NativeParallelHashMap<RecentKey, RecentValue> m_RecentMap;

		[ReadOnly]
		public FeedbackConfigurationData m_FeedbackConfigurationData;

		[ReadOnly]
		public DynamicBuffer<FeedbackLocalEffectFactor> m_FeedbackLocalEffectFactors;

		[ReadOnly]
		public DynamicBuffer<FeedbackCityEffectFactor> m_FeedbackCityEffectFactors;

		[ReadOnly]
		public CellMapData<TelecomCoverage> m_TelecomCoverageData;

		public IconCommandBuffer m_IconCommandBuffer;

		public NativeQueue<RecentUpdate>.ParallelWriter m_RecentUpdates;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<Transform> nativeArray2 = chunk.GetNativeArray(ref m_TransformType);
			NativeArray<Building> nativeArray3 = chunk.GetNativeArray(ref m_BuildingType);
			NativeArray<GarbageProducer> nativeArray4 = chunk.GetNativeArray(ref m_GarbageProducerType);
			NativeArray<ElectricityConsumer> nativeArray5 = chunk.GetNativeArray(ref m_ElectricityConsumerType);
			NativeArray<WaterConsumer> nativeArray6 = chunk.GetNativeArray(ref m_WaterConsumerType);
			NativeArray<MailProducer> nativeArray7 = chunk.GetNativeArray(ref m_MailProducerType);
			NativeArray<CrimeProducer> nativeArray8 = chunk.GetNativeArray(ref m_CrimeProducerType);
			CoverageData coverageData = default(CoverageData);
			if (m_PrefabCoverageData.HasComponent(m_FeedbackData.m_MainPrefab))
			{
				coverageData = m_PrefabCoverageData[m_FeedbackData.m_MainPrefab];
				if (m_FeedbackData.m_Prefab != m_FeedbackData.m_MainPrefab)
				{
					coverageData.m_Magnitude = 1f;
					coverageData.m_Service = CoverageService.Count;
				}
				else
				{
					coverageData.m_Magnitude = 1f / math.max(0.001f, coverageData.m_Magnitude);
				}
			}
			else
			{
				coverageData.m_Range = 25000f;
				coverageData.m_Magnitude = 1f;
				coverageData.m_Service = CoverageService.Count;
			}
			m_PrefabGarbageFacilityData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData);
			m_PrefabHospitalData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData2);
			m_PrefabDeathcareFacilityData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData3);
			m_PrefabPowerPlantData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData4);
			m_PrefabWindPoweredData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData5);
			m_PrefabSolarPoweredData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData6);
			m_PrefabWaterPumpingStationData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData7);
			m_PrefabSewageOutletData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData8);
			m_PrefabWastewaterTreatmentPlantData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData9);
			m_PrefabTransportDepotData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData10);
			m_PrefabTransportStationData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData11);
			m_PrefabTransportStopData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData12);
			m_PrefabPostFacilityData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData13);
			m_PrefabTelecomFacilityData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData14);
			m_PrefabSchoolData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData15);
			ParkingFacilityData componentData16;
			bool flag = m_PrefabParkingFacilityData.TryGetComponent(m_FeedbackData.m_Prefab, out componentData16);
			m_PrefabMaintenanceDepotData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData17);
			m_PrefabFireStationData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData18);
			m_PrefabPoliceStationData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData19);
			m_PrefabPrisonData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData20);
			m_PrefabPollutionData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData21);
			m_PrefabAttractionData.TryGetComponent(m_FeedbackData.m_Prefab, out var componentData22);
			bool flag2 = m_PrefabTransformerData.HasComponent(m_FeedbackData.m_Prefab);
			bool flag3 = m_PrefabPublicTransportStationData.HasComponent(m_FeedbackData.m_Prefab);
			bool flag4 = m_PrefabCargoTransportStationData.HasComponent(m_FeedbackData.m_Prefab);
			float num = 0f;
			NativeList<LocalModifierData> tempModifierList = default(NativeList<LocalModifierData>);
			NativeList<CityModifierData> tempModifierList2 = default(NativeList<CityModifierData>);
			if (m_PrefabLocalModifierDatas.TryGetBuffer(m_FeedbackData.m_Prefab, out var bufferData))
			{
				tempModifierList = new NativeList<LocalModifierData>(10, Allocator.Temp);
				LocalEffectSystem.InitializeTempList(tempModifierList, bufferData);
			}
			if (m_PrefabCityModifierDatas.TryGetBuffer(m_FeedbackData.m_Prefab, out var bufferData2))
			{
				tempModifierList2 = new NativeList<CityModifierData>(10, Allocator.Temp);
				CityModifierUpdateSystem.InitializeTempList(tempModifierList2, bufferData2);
			}
			if (m_FeedbackData.m_Prefab == m_FeedbackData.m_MainPrefab)
			{
				for (int i = 0; i < m_ExtraFeedbacks.Length; i++)
				{
					Entity prefab = m_ExtraFeedbacks[i].m_Prefab;
					if (m_PrefabGarbageFacilityData.TryGetComponent(prefab, out var componentData23))
					{
						componentData.Combine(componentData23);
					}
					if (m_PrefabHospitalData.TryGetComponent(prefab, out var componentData24))
					{
						componentData2.Combine(componentData24);
					}
					if (m_PrefabDeathcareFacilityData.TryGetComponent(prefab, out var componentData25))
					{
						componentData3.Combine(componentData25);
					}
					if (m_PrefabPowerPlantData.TryGetComponent(prefab, out var componentData26))
					{
						componentData4.Combine(componentData26);
					}
					if (m_PrefabWindPoweredData.TryGetComponent(prefab, out var componentData27))
					{
						componentData5.Combine(componentData27);
					}
					if (m_PrefabSolarPoweredData.TryGetComponent(prefab, out var componentData28))
					{
						componentData6.Combine(componentData28);
					}
					if (m_PrefabWaterPumpingStationData.TryGetComponent(prefab, out var componentData29))
					{
						componentData7.Combine(componentData29);
					}
					if (m_PrefabSewageOutletData.TryGetComponent(prefab, out var componentData30))
					{
						componentData8.Combine(componentData30);
					}
					if (m_PrefabWastewaterTreatmentPlantData.TryGetComponent(prefab, out var componentData31))
					{
						componentData9.Combine(componentData31);
					}
					if (m_PrefabTransportDepotData.TryGetComponent(prefab, out var componentData32))
					{
						componentData10.Combine(componentData32);
					}
					if (m_PrefabTransportStationData.TryGetComponent(prefab, out var componentData33))
					{
						componentData11.Combine(componentData33);
					}
					flag3 |= m_PrefabPublicTransportStationData.HasComponent(prefab);
					flag4 |= m_PrefabCargoTransportStationData.HasComponent(prefab);
					if (m_PrefabPostFacilityData.TryGetComponent(prefab, out var componentData34))
					{
						componentData13.Combine(componentData34);
					}
					if (m_PrefabTelecomFacilityData.TryGetComponent(prefab, out var componentData35))
					{
						componentData14.Combine(componentData35);
					}
					if (m_PrefabSchoolData.TryGetComponent(prefab, out var componentData36))
					{
						componentData15.Combine(componentData36);
					}
					if (m_PrefabParkingFacilityData.TryGetComponent(prefab, out var componentData37))
					{
						componentData16.Combine(componentData37);
						flag = true;
					}
					if (m_PrefabMaintenanceDepotData.TryGetComponent(prefab, out var componentData38))
					{
						componentData17.Combine(componentData38);
					}
					if (m_PrefabFireStationData.TryGetComponent(prefab, out var componentData39))
					{
						componentData18.Combine(componentData39);
					}
					if (m_PrefabPoliceStationData.TryGetComponent(prefab, out var componentData40))
					{
						componentData19.Combine(componentData40);
					}
					if (m_PrefabPrisonData.TryGetComponent(prefab, out var componentData41))
					{
						componentData20.Combine(componentData41);
					}
					if (m_PrefabPollutionData.TryGetComponent(prefab, out var componentData42))
					{
						componentData21.Combine(componentData42);
					}
					if (m_PrefabAttractionData.TryGetComponent(prefab, out var componentData43))
					{
						componentData22.Combine(componentData43);
					}
					if (m_PrefabLocalModifierDatas.TryGetBuffer(m_FeedbackData.m_Prefab, out var bufferData3))
					{
						LocalEffectSystem.AddToTempList(tempModifierList, bufferData3, disabled: false);
					}
					if (m_PrefabCityModifierDatas.TryGetBuffer(m_FeedbackData.m_Prefab, out var bufferData4))
					{
						CityModifierUpdateSystem.AddToTempList(tempModifierList2, bufferData4);
					}
				}
			}
			else
			{
				bool flag5 = componentData14.m_Range >= 1f;
				bool flag6 = componentData14.m_NetworkCapacity >= 1f;
				if (flag5 || flag6)
				{
					m_PrefabTelecomFacilityData.TryGetComponent(m_FeedbackData.m_MainPrefab, out var componentData44);
					for (int j = 0; j < m_ExtraFeedbacks.Length; j++)
					{
						Entity prefab2 = m_ExtraFeedbacks[j].m_Prefab;
						if (m_PrefabTelecomFacilityData.TryGetComponent(prefab2, out var componentData45))
						{
							componentData44.Combine(componentData45);
						}
					}
					if (flag5)
					{
						componentData14.m_NetworkCapacity += componentData44.m_NetworkCapacity;
					}
					componentData14.m_Range += componentData44.m_Range;
					if (flag5 && !flag6)
					{
						num = componentData44.m_Range;
					}
				}
				bool flag7 = componentData18.m_FireEngineCapacity != 0;
				bool flag8 = componentData18.m_FireHelicopterCapacity != 0;
				bool flag9 = componentData18.m_DisasterResponseCapacity != 0;
				bool flag10 = componentData18.m_VehicleEfficiency != 0f;
				if (flag7 || flag8 || flag9 || flag10)
				{
					m_PrefabFireStationData.TryGetComponent(m_FeedbackData.m_MainPrefab, out var componentData46);
					for (int k = 0; k < m_ExtraFeedbacks.Length; k++)
					{
						Entity prefab3 = m_ExtraFeedbacks[k].m_Prefab;
						if (m_PrefabFireStationData.TryGetComponent(prefab3, out var componentData47))
						{
							componentData46.Combine(componentData47);
						}
					}
					if (flag7 || flag8)
					{
						componentData18.m_VehicleEfficiency += componentData46.m_VehicleEfficiency;
					}
					if (flag9 || flag10)
					{
						componentData18.m_FireEngineCapacity += componentData46.m_FireEngineCapacity;
						componentData18.m_FireHelicopterCapacity += componentData46.m_FireHelicopterCapacity;
					}
				}
				bool flag11 = (float)componentData17.m_VehicleCapacity != 0f;
				bool flag12 = componentData17.m_VehicleEfficiency != 0f;
				if (flag11 || flag12)
				{
					m_PrefabMaintenanceDepotData.TryGetComponent(m_FeedbackData.m_MainPrefab, out var componentData48);
					for (int l = 0; l < m_ExtraFeedbacks.Length; l++)
					{
						Entity prefab4 = m_ExtraFeedbacks[l].m_Prefab;
						if (m_PrefabMaintenanceDepotData.TryGetComponent(prefab4, out var componentData49))
						{
							componentData48.Combine(componentData49);
						}
					}
					if (flag11)
					{
						componentData17.m_VehicleEfficiency += componentData48.m_VehicleEfficiency;
					}
					if (flag12)
					{
						componentData17.m_VehicleCapacity += componentData48.m_VehicleCapacity;
					}
				}
				if (componentData10.m_DispatchCenter)
				{
					m_PrefabTransportDepotData.TryGetComponent(m_FeedbackData.m_MainPrefab, out var componentData50);
					for (int m = 0; m < m_ExtraFeedbacks.Length; m++)
					{
						Entity prefab5 = m_ExtraFeedbacks[m].m_Prefab;
						if (m_PrefabTransportDepotData.TryGetComponent(prefab5, out var componentData51))
						{
							componentData50.Combine(componentData51);
						}
					}
					componentData10.m_VehicleCapacity += componentData50.m_VehicleCapacity;
				}
				if (tempModifierList.IsCreated)
				{
					if (m_PrefabLocalModifierDatas.TryGetBuffer(m_FeedbackData.m_MainPrefab, out var bufferData5))
					{
						AddToTempListForUpgrade(tempModifierList, bufferData5);
					}
					for (int n = 0; n < m_ExtraFeedbacks.Length; n++)
					{
						Entity prefab6 = m_ExtraFeedbacks[n].m_Prefab;
						if (m_PrefabLocalModifierDatas.TryGetBuffer(prefab6, out var bufferData6))
						{
							AddToTempListForUpgrade(tempModifierList, bufferData6);
						}
					}
				}
			}
			Unity.Mathematics.Random random = m_RandomSeed.GetRandom(unfilteredChunkIndex);
			NativeList<RecentUpdate> updateList = default(NativeList<RecentUpdate>);
			for (int num2 = 0; num2 < nativeArray.Length; num2++)
			{
				Entity entity = nativeArray[num2];
				Transform transform = nativeArray2[num2];
				Building building = nativeArray3[num2];
				if (entity == m_FeedbackData.m_MainEntity)
				{
					continue;
				}
				float3 total = 0f;
				float num4;
				float num3;
				if (m_CoverageMap.IsCreated && m_CoverageMap.TryGetValue(building.m_RoadEdge, out var item))
				{
					num3 = math.lerp(item.x, item.y, building.m_CurvePosition);
					num4 = math.max(0f, 1f - num3 * num3);
					num3 *= coverageData.m_Range;
					if (num4 != 0f)
					{
						if (coverageData.m_Service != CoverageService.Count && m_ServiceCoverages.TryGetBuffer(building.m_RoadEdge, out var bufferData7) && bufferData7.Length != 0)
						{
							Game.Net.ServiceCoverage serviceCoverage = bufferData7[(int)coverageData.m_Service];
							float num5 = 1f + math.lerp(serviceCoverage.m_Coverage.x, serviceCoverage.m_Coverage.y, building.m_CurvePosition) * coverageData.m_Magnitude;
							float delta = num4 / math.max(num4, num5 * num5);
							AddEffect(ref updateList, ref total, new RecentKey(entity, coverageData.m_Service), delta, num3);
						}
						if (componentData.m_VehicleCapacity != 0 && nativeArray4.Length != 0)
						{
							float delta2 = num4 * math.saturate((float)nativeArray4[num2].m_Garbage * m_FeedbackConfigurationData.m_GarbageProducerGarbageFactor) * math.saturate((float)componentData.m_VehicleCapacity * m_FeedbackConfigurationData.m_GarbageVehicleFactor);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.GarbageVehicles), delta2, num3);
						}
						if (componentData2.m_AmbulanceCapacity != 0)
						{
							float delta3 = num4 * math.saturate((float)componentData2.m_AmbulanceCapacity * m_FeedbackConfigurationData.m_HospitalAmbulanceFactor);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.HospitalAmbulances), delta3, num3);
						}
						if (componentData2.m_PatientCapacity != 0)
						{
							float delta4 = num4 * math.saturate((float)componentData2.m_PatientCapacity * m_FeedbackConfigurationData.m_HospitalCapacityFactor);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.HospitalCapacity), delta4, num3);
						}
						if (componentData3.m_HearseCapacity != 0)
						{
							float delta5 = num4 * math.saturate((float)componentData3.m_HearseCapacity * m_FeedbackConfigurationData.m_DeathcareHearseFactor);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.DeathcareHearses), delta5, num3);
						}
						if (componentData3.m_StorageCapacity != 0 || componentData3.m_ProcessingRate != 0f)
						{
							float delta6 = num4 * math.saturate((float)componentData3.m_StorageCapacity * m_FeedbackConfigurationData.m_DeathcareCapacityFactor + componentData3.m_ProcessingRate * m_FeedbackConfigurationData.m_DeathcareProcessingFactor);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.DeathcareCapacity), delta6, num3);
						}
						if (flag2 && nativeArray5.Length != 0)
						{
							ElectricityConsumer electricityConsumer = nativeArray5[num2];
							float falseValue = math.saturate((float)electricityConsumer.m_WantedConsumption * m_FeedbackConfigurationData.m_ElectricityConsumptionFactor);
							float num6 = num4 * math.select(falseValue, 1f, !electricityConsumer.electricityConnected);
							num6 *= math.saturate(1f - num3 / m_FeedbackConfigurationData.m_TransformerRadius);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.Transformer), num6, num3);
						}
						if (componentData10.m_DispatchCenter)
						{
							float delta7 = num4 * math.saturate((float)componentData10.m_VehicleCapacity * m_FeedbackConfigurationData.m_TransportDispatchCenterFactor);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.TransportDispatch), delta7, num3);
						}
						if (flag3)
						{
							float num7 = num4 * math.saturate(0.5f + componentData11.m_ComfortFactor * 0.5f);
							num7 *= math.saturate(1f - num3 / m_FeedbackConfigurationData.m_TransportStationRange);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.PublicTransport), num7, num3);
						}
						if (flag4)
						{
							float num8 = num4 * math.saturate(0.5f + componentData11.m_LoadingFactor * 0.5f);
							num8 *= math.saturate(1f - num3 / m_FeedbackConfigurationData.m_TransportStationRange);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.CargoTransport), num8, num3);
						}
						if (componentData12.m_PassengerTransport)
						{
							float num9 = num4 * math.saturate(0.5f + componentData12.m_ComfortFactor * 0.5f);
							num9 *= math.saturate(1f - num3 / m_FeedbackConfigurationData.m_TransportStopRange);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.PublicTransport), num9, num3);
						}
						if ((componentData13.m_PostVanCapacity != 0 || componentData13.m_PostTruckCapacity != 0) && nativeArray7.Length != 0)
						{
							MailProducer mailProducer = nativeArray7[num2];
							float delta8 = num4 * math.saturate((float)(mailProducer.receivingMail + mailProducer.m_SendingMail) * m_FeedbackConfigurationData.m_MailProducerMailFactor) * math.saturate((float)componentData13.m_PostVanCapacity * m_FeedbackConfigurationData.m_PostFacilityVanFactor + (float)componentData13.m_PostTruckCapacity * m_FeedbackConfigurationData.m_PostFacilityTruckFactor);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.PostFacilityVehicles), delta8, num3);
						}
						if ((componentData13.m_MailCapacity != 0 || componentData13.m_SortingRate != 0) && nativeArray7.Length != 0)
						{
							MailProducer mailProducer2 = nativeArray7[num2];
							float delta9 = num4 * math.saturate((float)(mailProducer2.receivingMail + mailProducer2.m_SendingMail) * m_FeedbackConfigurationData.m_MailProducerMailFactor) * math.saturate((float)componentData13.m_MailCapacity * m_FeedbackConfigurationData.m_PostFacilityCapacityFactor + (float)componentData13.m_SortingRate * m_FeedbackConfigurationData.m_PostFacilityProcessingFactor);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.PostFacilityCapacity), delta9, num3);
						}
						if (componentData15.m_StudentCapacity != 0)
						{
							float num10 = 0f;
							FeedbackType feedbackType = FeedbackType.GarbageVehicles;
							switch ((SchoolLevel)componentData15.m_EducationLevel)
							{
							case SchoolLevel.Elementary:
								num10 = m_FeedbackConfigurationData.m_ElementarySchoolCapacityFactor;
								feedbackType = FeedbackType.ElementarySchoolCapacity;
								break;
							case SchoolLevel.HighSchool:
								num10 = m_FeedbackConfigurationData.m_HighSchoolCapacityFactor;
								feedbackType = FeedbackType.HighSchoolCapacity;
								break;
							case SchoolLevel.College:
								num10 = m_FeedbackConfigurationData.m_CollegeCapacityFactor;
								feedbackType = FeedbackType.CollegeCapacity;
								break;
							case SchoolLevel.University:
								num10 = m_FeedbackConfigurationData.m_UniversityCapacityFactor;
								feedbackType = FeedbackType.UniversityCapacity;
								break;
							}
							if (num10 != 0f)
							{
								float delta10 = num4 * math.saturate((float)componentData15.m_StudentCapacity * num10);
								AddEffect(ref updateList, ref total, new RecentKey(entity, feedbackType), delta10, num3);
							}
						}
						if (flag)
						{
							float num11 = num4 * math.saturate(0.5f + componentData16.m_ComfortFactor * 0.5f);
							num11 *= math.saturate(1f - num3 / m_FeedbackConfigurationData.m_ParkingFacilityRange);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.ParkingSpaces), num11, num3);
						}
						if (componentData17.m_VehicleCapacity != 0)
						{
							float num12 = (float)componentData17.m_VehicleCapacity * componentData17.m_VehicleEfficiency;
							float delta11 = num4 * math.saturate(num12 * m_FeedbackConfigurationData.m_MaintenanceVehicleFactor);
							AddEffect(ref updateList, ref total, new RecentKey(entity, componentData17.m_MaintenanceType), delta11, num3);
						}
						if (componentData18.m_FireEngineCapacity != 0)
						{
							float num13 = (float)componentData18.m_FireEngineCapacity * componentData18.m_VehicleEfficiency;
							num13 += (float)math.min(componentData18.m_FireEngineCapacity, componentData18.m_DisasterResponseCapacity);
							float delta12 = num4 * math.saturate(num13 * m_FeedbackConfigurationData.m_FireStationEngineFactor);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.FireStationEngines), delta12, num3);
						}
						if (componentData19.m_PatrolCarCapacity != 0 && nativeArray8.Length != 0)
						{
							float delta13 = num4 * math.saturate(nativeArray8[num2].m_Crime * m_FeedbackConfigurationData.m_CrimeProducerCrimeFactor) * math.saturate((float)componentData19.m_PatrolCarCapacity * m_FeedbackConfigurationData.m_PoliceStationCarFactor);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.PoliceStationCars), delta13, num3);
						}
						if (componentData19.m_JailCapacity != 0 && nativeArray8.Length != 0)
						{
							float delta14 = num4 * math.saturate(nativeArray8[num2].m_Crime * m_FeedbackConfigurationData.m_CrimeProducerCrimeFactor) * math.saturate((float)componentData19.m_JailCapacity * m_FeedbackConfigurationData.m_PoliceStationCapacityFactor);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.PoliceStationCapacity), delta14, num3);
						}
						if (componentData20.m_PrisonVanCapacity != 0 && nativeArray8.Length != 0)
						{
							float delta15 = num4 * math.saturate(nativeArray8[num2].m_Crime * m_FeedbackConfigurationData.m_CrimeProducerCrimeFactor) * math.saturate((float)componentData20.m_PrisonVanCapacity * m_FeedbackConfigurationData.m_PrisonVehicleFactor);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.PrisonVehicles), delta15, num3);
						}
						if (componentData20.m_PrisonerCapacity != 0 && nativeArray8.Length != 0)
						{
							float delta16 = num4 * math.saturate(nativeArray8[num2].m_Crime * m_FeedbackConfigurationData.m_CrimeProducerCrimeFactor) * math.saturate((float)componentData20.m_PrisonerCapacity * m_FeedbackConfigurationData.m_PrisonCapacityFactor);
							AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.PrisonCapacity), delta16, num3);
						}
					}
				}
				num4 = 1f;
				num3 = math.distance(transform.m_Position.xz, m_FeedbackData.m_Position.xz);
				if (componentData2.m_MedicalHelicopterCapacity != 0)
				{
					float delta17 = num4 * math.saturate((float)componentData2.m_MedicalHelicopterCapacity * m_FeedbackConfigurationData.m_HospitalHelicopterFactor);
					AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.HospitalHelicopters), delta17, num3);
				}
				if ((componentData4.m_ElectricityProduction != 0 || componentData5.m_Production != 0 || componentData6.m_Production != 0) && nativeArray5.Length != 0)
				{
					ElectricityConsumer electricityConsumer2 = nativeArray5[num2];
					float falseValue2 = math.saturate((float)electricityConsumer2.m_WantedConsumption * m_FeedbackConfigurationData.m_ElectricityConsumptionFactor);
					float num14 = componentData4.m_ElectricityProduction + componentData5.m_Production + componentData6.m_Production;
					float delta18 = num4 * math.select(falseValue2, 1f, !electricityConsumer2.electricityConnected) * math.saturate(num14 * m_FeedbackConfigurationData.m_ElectricityProductionFactor);
					AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.Electricity), delta18, num3);
				}
				if (componentData7.m_Capacity != 0 && nativeArray6.Length != 0)
				{
					WaterConsumer waterConsumer = nativeArray6[num2];
					float falseValue3 = math.saturate((float)waterConsumer.m_WantedConsumption * m_FeedbackConfigurationData.m_WaterConsumptionFactor);
					float num15 = componentData7.m_Capacity;
					float delta19 = num4 * math.select(falseValue3, 1f, !waterConsumer.waterConnected) * math.saturate(num15 * m_FeedbackConfigurationData.m_WaterCapacityFactor);
					AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.WaterCapacity), delta19, num3);
				}
				if ((componentData8.m_Capacity != 0 || componentData9.m_Capacity != 0) && nativeArray6.Length != 0)
				{
					WaterConsumer waterConsumer2 = nativeArray6[num2];
					float falseValue4 = math.saturate((float)waterConsumer2.m_WantedConsumption * m_FeedbackConfigurationData.m_WaterConsumerSewageFactor);
					float num16 = componentData8.m_Capacity + componentData9.m_Capacity;
					float delta20 = num4 * math.select(falseValue4, 1f, !waterConsumer2.sewageConnected) * math.saturate(num16 * m_FeedbackConfigurationData.m_SewageCapacityFactor);
					AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.SewageCapacity), delta20, num3);
				}
				if (componentData10.m_VehicleCapacity != 0)
				{
					float delta21 = num4 * math.saturate((float)componentData10.m_VehicleCapacity * m_FeedbackConfigurationData.m_TransportVehicleCapacityFactor);
					AddEffect(ref updateList, ref total, new RecentKey(entity, componentData10.m_TransportType), delta21, num3);
				}
				if (componentData14.m_NetworkCapacity >= 1f && componentData14.m_Range >= 1f)
				{
					float num17 = num4 * math.saturate(componentData14.m_NetworkCapacity * m_FeedbackConfigurationData.m_TelecomCapacityFactor);
					num17 *= math.saturate(1f - num3 / componentData14.m_Range);
					if (num >= 1f)
					{
						num17 *= math.saturate(num3 / num);
					}
					if (num17 != 0f)
					{
						float y = 1f + TelecomCoverage.SampleNetworkQuality(m_TelecomCoverageData, transform.m_Position);
						num17 /= math.max(num17, y);
						AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.TelecomCoverage), num17, num3);
					}
				}
				if (componentData18.m_FireHelicopterCapacity != 0)
				{
					float num18 = (float)componentData18.m_FireHelicopterCapacity * componentData18.m_VehicleEfficiency;
					num18 += (float)math.min(componentData18.m_FireHelicopterCapacity, componentData18.m_DisasterResponseCapacity);
					float delta22 = num4 * math.saturate(num18 * m_FeedbackConfigurationData.m_FireStationHelicopterFactor);
					AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.FireStationHelicopters), delta22, num3);
				}
				if (componentData19.m_PoliceHelicopterCapacity != 0 && nativeArray8.Length != 0)
				{
					float delta23 = num4 * math.saturate(nativeArray8[num2].m_Crime * m_FeedbackConfigurationData.m_CrimeProducerCrimeFactor) * math.saturate((float)componentData19.m_PoliceHelicopterCapacity * m_FeedbackConfigurationData.m_PoliceStationHelicopterFactor);
					AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.PoliceStationHelicopters), delta23, num3);
				}
				if (componentData21.m_GroundPollution != 0f || componentData21.m_AirPollution != 0f || componentData21.m_NoisePollution != 0f)
				{
					float3 x = num4 * new float3(componentData21.m_GroundPollution, componentData21.m_AirPollution, componentData21.m_NoisePollution);
					x *= new float3(m_FeedbackConfigurationData.m_GroundPollutionFactor, m_FeedbackConfigurationData.m_AirPollutionFactor, m_FeedbackConfigurationData.m_NoisePollutionFactor);
					x = math.saturate(x);
					x *= 1f - num3 / new float3(m_FeedbackConfigurationData.m_GroundPollutionRadius, m_FeedbackConfigurationData.m_AirPollutionRadius, m_FeedbackConfigurationData.m_NoisePollutionRadius);
					x = -math.saturate(x);
					if (x.x != 0f)
					{
						AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.GroundPollution), x.x, num3);
					}
					if (x.y != 0f)
					{
						AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.AirPollution), x.y, num3);
					}
					if (x.z != 0f)
					{
						AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.NoisePollution), x.z, num3);
					}
				}
				if (componentData22.m_Attractiveness != 0)
				{
					float delta24 = num4 * math.saturate((float)componentData22.m_Attractiveness * m_FeedbackConfigurationData.m_AttractivenessFactor);
					AddEffect(ref updateList, ref total, new RecentKey(entity, FeedbackType.Attractiveness), delta24, num3);
				}
				if (tempModifierList.IsCreated)
				{
					for (int num19 = 0; num19 < tempModifierList.Length; num19++)
					{
						LocalModifierData localModifierData = tempModifierList[num19];
						if (m_FeedbackLocalEffectFactors.Length > (int)localModifierData.m_Type)
						{
							float factor = m_FeedbackLocalEffectFactors[(int)localModifierData.m_Type].m_Factor;
							factor = math.select(math.sign(factor), factor, localModifierData.m_Mode == ModifierValueMode.Absolute);
							float num20 = num4 * math.clamp(localModifierData.m_Delta.max * factor, -1f, 1f);
							num20 *= math.saturate(1f - num3 / localModifierData.m_Radius.max);
							if (localModifierData.m_Radius.min != 0f)
							{
								num20 *= math.saturate(num3 / localModifierData.m_Radius.min);
							}
							if (num20 != 0f)
							{
								AddEffect(ref updateList, ref total, new RecentKey(entity, localModifierData.m_Type), num20, num3);
							}
						}
					}
				}
				if (tempModifierList2.IsCreated)
				{
					for (int num21 = 0; num21 < tempModifierList2.Length; num21++)
					{
						CityModifierData cityModifierData = tempModifierList2[num21];
						if (m_FeedbackCityEffectFactors.Length > (int)cityModifierData.m_Type)
						{
							float factor2 = m_FeedbackCityEffectFactors[(int)cityModifierData.m_Type].m_Factor;
							factor2 = math.select(math.sign(factor2), factor2, cityModifierData.m_Mode == ModifierValueMode.Absolute);
							float num22 = num4 * math.clamp(cityModifierData.m_Range.max * factor2, -1f, 1f);
							if (num22 != 0f)
							{
								AddEffect(ref updateList, ref total, new RecentKey(entity, cityModifierData.m_Type), num22, num3);
							}
						}
					}
				}
				if (random.NextFloat(1f) < math.abs(total.x))
				{
					bool flag13 = total.x > 0f;
					num3 = total.y / total.z;
					Entity prefab7 = (flag13 ? m_FeedbackConfigurationData.m_HappyFaceNotification : m_FeedbackConfigurationData.m_SadFaceNotification);
					float delay = num3 * 0.001f + random.NextFloat(0.1f);
					m_IconCommandBuffer.Add(entity, prefab7, IconPriority.Info, IconClusterLayer.Transaction, (IconFlags)0, Entity.Null, isTemp: false, isHidden: false, disallowCluster: false, delay);
					if (updateList.IsCreated)
					{
						for (int num23 = 0; num23 < updateList.Length; num23++)
						{
							RecentUpdate value = updateList[num23];
							if (value.m_Delta > 0f == flag13)
							{
								m_RecentUpdates.Enqueue(value);
							}
						}
					}
				}
				if (updateList.IsCreated)
				{
					updateList.Clear();
				}
			}
			if (updateList.IsCreated)
			{
				updateList.Dispose();
			}
			if (tempModifierList.IsCreated)
			{
				tempModifierList.Dispose();
			}
			if (tempModifierList2.IsCreated)
			{
				tempModifierList2.Dispose();
			}
		}

		private void AddToTempListForUpgrade(NativeList<LocalModifierData> tempModifierList, DynamicBuffer<LocalModifierData> localModifiers)
		{
			for (int i = 0; i < localModifiers.Length; i++)
			{
				LocalModifierData localModifierData = localModifiers[i];
				for (int j = 0; j < tempModifierList.Length; j++)
				{
					LocalModifierData value = tempModifierList[j];
					if (value.m_Type != localModifierData.m_Type)
					{
						continue;
					}
					bool flag = value.m_Radius.max > 0f;
					bool flag2 = value.m_Delta.max != 0f;
					if (flag)
					{
						value.m_Delta.max += localModifierData.m_Delta.max;
					}
					switch (value.m_RadiusCombineMode)
					{
					case ModifierRadiusCombineMode.Additive:
						if (flag && !flag2)
						{
							value.m_Radius.min += localModifierData.m_Radius.max;
						}
						value.m_Radius.max += localModifierData.m_Radius.max;
						break;
					case ModifierRadiusCombineMode.Maximal:
						if (flag && !flag2)
						{
							value.m_Radius.min = math.max(value.m_Radius.min, localModifierData.m_Radius.max);
						}
						value.m_Radius.max = math.max(value.m_Radius.max, localModifierData.m_Radius.max);
						break;
					}
					tempModifierList[j] = value;
					break;
				}
			}
		}

		private void AddEffect(ref NativeList<RecentUpdate> updateList, ref float3 total, RecentKey recentKey, float delta, float distance)
		{
			delta = math.select(delta, 0f - delta, m_FeedbackData.m_IsDeleted);
			RecentUpdate value = new RecentUpdate
			{
				m_Key = recentKey,
				m_Delta = math.sign(delta)
			};
			if (m_RecentMap.TryGetValue(recentKey, out var item))
			{
				float valueToClamp = delta - item.m_FeedbackDelta;
				delta = math.select(math.clamp(valueToClamp, delta, 0f), math.clamp(valueToClamp, 0f, delta), delta > 0f);
			}
			if (delta != 0f)
			{
				float num = math.abs(delta);
				total += new float3(delta, distance * num, num);
				if (!updateList.IsCreated)
				{
					updateList = new NativeList<RecentUpdate>(10, Allocator.Temp);
				}
				updateList.Add(in value);
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct UpdateRecentMapJob : IJob
	{
		public NativeParallelHashMap<RecentKey, RecentValue> m_RecentMap;

		public NativeQueue<RecentUpdate> m_RecentUpdates;

		public uint m_SimulationFrame;

		public void Execute()
		{
			NativeArray<RecentKey> keyArray = m_RecentMap.GetKeyArray(Allocator.Temp);
			for (int i = 0; i < keyArray.Length; i++)
			{
				RecentKey key = keyArray[i];
				RecentValue value = m_RecentMap[key];
				float num = (float)(m_SimulationFrame - value.m_UpdateFrame) * 0.0001f;
				value.m_UpdateFrame = m_SimulationFrame;
				value.m_FeedbackDelta = math.select(math.min(0f, value.m_FeedbackDelta + num), math.max(0f, value.m_FeedbackDelta - num), value.m_FeedbackDelta > 0f);
				if (value.m_FeedbackDelta != 0f)
				{
					m_RecentMap[key] = value;
				}
				else
				{
					m_RecentMap.Remove(key);
				}
			}
			keyArray.Dispose();
			RecentUpdate item;
			while (m_RecentUpdates.TryDequeue(out item))
			{
				if (m_RecentMap.TryGetValue(item.m_Key, out var item2))
				{
					item2.m_FeedbackDelta += item.m_Delta;
					if (item2.m_FeedbackDelta != 0f)
					{
						m_RecentMap[item.m_Key] = item2;
					}
					else
					{
						m_RecentMap.Remove(item.m_Key);
					}
				}
				else
				{
					m_RecentMap.Add(item.m_Key, new RecentValue
					{
						m_UpdateFrame = m_SimulationFrame,
						m_FeedbackDelta = item.m_Delta
					});
				}
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentLookup<BackSide> __Game_Buildings_BackSide_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CoverageData> __Game_Prefabs_CoverageData_RO_ComponentLookup;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Transform> __Game_Objects_Transform_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<Building> __Game_Buildings_Building_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<GarbageProducer> __Game_Buildings_GarbageProducer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityConsumer> __Game_Buildings_ElectricityConsumer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterConsumer> __Game_Buildings_WaterConsumer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MailProducer> __Game_Buildings_MailProducer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<CrimeProducer> __Game_Buildings_CrimeProducer_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentLookup<GarbageFacilityData> __Game_Prefabs_GarbageFacilityData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<HospitalData> __Game_Prefabs_HospitalData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<DeathcareFacilityData> __Game_Prefabs_DeathcareFacilityData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PowerPlantData> __Game_Prefabs_PowerPlantData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WindPoweredData> __Game_Prefabs_WindPoweredData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SolarPoweredData> __Game_Prefabs_SolarPoweredData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<Game.Prefabs.TransformerData> __Game_Prefabs_TransformerData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WaterPumpingStationData> __Game_Prefabs_WaterPumpingStationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SewageOutletData> __Game_Prefabs_SewageOutletData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<WastewaterTreatmentPlantData> __Game_Prefabs_WastewaterTreatmentPlantData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportDepotData> __Game_Prefabs_TransportDepotData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportStationData> __Game_Prefabs_TransportStationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PublicTransportStationData> __Game_Prefabs_PublicTransportStationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<CargoTransportStationData> __Game_Prefabs_CargoTransportStationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportStopData> __Game_Prefabs_TransportStopData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PostFacilityData> __Game_Prefabs_PostFacilityData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TelecomFacilityData> __Game_Prefabs_TelecomFacilityData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<SchoolData> __Game_Prefabs_SchoolData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ParkingFacilityData> __Game_Prefabs_ParkingFacilityData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<MaintenanceDepotData> __Game_Prefabs_MaintenanceDepotData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<FireStationData> __Game_Prefabs_FireStationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PoliceStationData> __Game_Prefabs_PoliceStationData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PrisonData> __Game_Prefabs_PrisonData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<PollutionData> __Game_Prefabs_PollutionData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<AttractionData> __Game_Prefabs_AttractionData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<Game.Net.ServiceCoverage> __Game_Net_ServiceCoverage_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<LocalModifierData> __Game_Prefabs_LocalModifierData_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<CityModifierData> __Game_Prefabs_CityModifierData_RO_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Buildings_BackSide_RO_ComponentLookup = state.GetComponentLookup<BackSide>(isReadOnly: true);
			__Game_Prefabs_CoverageData_RO_ComponentLookup = state.GetComponentLookup<CoverageData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Objects_Transform_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Transform>(isReadOnly: true);
			__Game_Buildings_Building_RO_ComponentTypeHandle = state.GetComponentTypeHandle<Building>(isReadOnly: true);
			__Game_Buildings_GarbageProducer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GarbageProducer>(isReadOnly: true);
			__Game_Buildings_ElectricityConsumer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityConsumer>(isReadOnly: true);
			__Game_Buildings_WaterConsumer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterConsumer>(isReadOnly: true);
			__Game_Buildings_MailProducer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MailProducer>(isReadOnly: true);
			__Game_Buildings_CrimeProducer_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CrimeProducer>(isReadOnly: true);
			__Game_Prefabs_GarbageFacilityData_RO_ComponentLookup = state.GetComponentLookup<GarbageFacilityData>(isReadOnly: true);
			__Game_Prefabs_HospitalData_RO_ComponentLookup = state.GetComponentLookup<HospitalData>(isReadOnly: true);
			__Game_Prefabs_DeathcareFacilityData_RO_ComponentLookup = state.GetComponentLookup<DeathcareFacilityData>(isReadOnly: true);
			__Game_Prefabs_PowerPlantData_RO_ComponentLookup = state.GetComponentLookup<PowerPlantData>(isReadOnly: true);
			__Game_Prefabs_WindPoweredData_RO_ComponentLookup = state.GetComponentLookup<WindPoweredData>(isReadOnly: true);
			__Game_Prefabs_SolarPoweredData_RO_ComponentLookup = state.GetComponentLookup<SolarPoweredData>(isReadOnly: true);
			__Game_Prefabs_TransformerData_RO_ComponentLookup = state.GetComponentLookup<Game.Prefabs.TransformerData>(isReadOnly: true);
			__Game_Prefabs_WaterPumpingStationData_RO_ComponentLookup = state.GetComponentLookup<WaterPumpingStationData>(isReadOnly: true);
			__Game_Prefabs_SewageOutletData_RO_ComponentLookup = state.GetComponentLookup<SewageOutletData>(isReadOnly: true);
			__Game_Prefabs_WastewaterTreatmentPlantData_RO_ComponentLookup = state.GetComponentLookup<WastewaterTreatmentPlantData>(isReadOnly: true);
			__Game_Prefabs_TransportDepotData_RO_ComponentLookup = state.GetComponentLookup<TransportDepotData>(isReadOnly: true);
			__Game_Prefabs_TransportStationData_RO_ComponentLookup = state.GetComponentLookup<TransportStationData>(isReadOnly: true);
			__Game_Prefabs_PublicTransportStationData_RO_ComponentLookup = state.GetComponentLookup<PublicTransportStationData>(isReadOnly: true);
			__Game_Prefabs_CargoTransportStationData_RO_ComponentLookup = state.GetComponentLookup<CargoTransportStationData>(isReadOnly: true);
			__Game_Prefabs_TransportStopData_RO_ComponentLookup = state.GetComponentLookup<TransportStopData>(isReadOnly: true);
			__Game_Prefabs_PostFacilityData_RO_ComponentLookup = state.GetComponentLookup<PostFacilityData>(isReadOnly: true);
			__Game_Prefabs_TelecomFacilityData_RO_ComponentLookup = state.GetComponentLookup<TelecomFacilityData>(isReadOnly: true);
			__Game_Prefabs_SchoolData_RO_ComponentLookup = state.GetComponentLookup<SchoolData>(isReadOnly: true);
			__Game_Prefabs_ParkingFacilityData_RO_ComponentLookup = state.GetComponentLookup<ParkingFacilityData>(isReadOnly: true);
			__Game_Prefabs_MaintenanceDepotData_RO_ComponentLookup = state.GetComponentLookup<MaintenanceDepotData>(isReadOnly: true);
			__Game_Prefabs_FireStationData_RO_ComponentLookup = state.GetComponentLookup<FireStationData>(isReadOnly: true);
			__Game_Prefabs_PoliceStationData_RO_ComponentLookup = state.GetComponentLookup<PoliceStationData>(isReadOnly: true);
			__Game_Prefabs_PrisonData_RO_ComponentLookup = state.GetComponentLookup<PrisonData>(isReadOnly: true);
			__Game_Prefabs_PollutionData_RO_ComponentLookup = state.GetComponentLookup<PollutionData>(isReadOnly: true);
			__Game_Prefabs_AttractionData_RO_ComponentLookup = state.GetComponentLookup<AttractionData>(isReadOnly: true);
			__Game_Net_ServiceCoverage_RO_BufferLookup = state.GetBufferLookup<Game.Net.ServiceCoverage>(isReadOnly: true);
			__Game_Prefabs_LocalModifierData_RO_BufferLookup = state.GetBufferLookup<LocalModifierData>(isReadOnly: true);
			__Game_Prefabs_CityModifierData_RO_BufferLookup = state.GetBufferLookup<CityModifierData>(isReadOnly: true);
		}
	}

	private const float INFINITE_RANGE = 25000f;

	private IconCommandSystem m_IconCommandSystem;

	private PathfindQueueSystem m_PathfindQueueSystem;

	private AirwaySystem m_AirwaySystem;

	private SimulationSystem m_SimulationSystem;

	private TelecomCoverageSystem m_TelecomCoverageSystem;

	private List<Entity> m_FeedbackContainers;

	private List<Entity> m_PendingContainers;

	private NativeParallelHashMap<RecentKey, RecentValue> m_RecentMap;

	private PathfindTargetSeekerData m_TargetSeekerData;

	private EntityQuery m_ConfigurationQuery;

	private EntityQuery m_AppliedQuery;

	private EntityQuery m_TargetQuery;

	private EntityQuery m_EventQuery;

	private JobHandle m_RecentDeps;

	private TypeHandle __TypeHandle;

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_IconCommandSystem = base.World.GetOrCreateSystemManaged<IconCommandSystem>();
		m_PathfindQueueSystem = base.World.GetOrCreateSystemManaged<PathfindQueueSystem>();
		m_AirwaySystem = base.World.GetOrCreateSystemManaged<AirwaySystem>();
		m_SimulationSystem = base.World.GetOrCreateSystemManaged<SimulationSystem>();
		m_TelecomCoverageSystem = base.World.GetOrCreateSystemManaged<TelecomCoverageSystem>();
		m_FeedbackContainers = new List<Entity>();
		m_PendingContainers = new List<Entity>();
		m_RecentMap = new NativeParallelHashMap<RecentKey, RecentValue>(1000, Allocator.Persistent);
		m_TargetSeekerData = new PathfindTargetSeekerData(this);
		m_ConfigurationQuery = GetEntityQuery(ComponentType.ReadOnly<FeedbackConfigurationData>());
		m_AppliedQuery = GetEntityQuery(new EntityQueryDesc
		{
			All = new ComponentType[3]
			{
				ComponentType.ReadOnly<Applied>(),
				ComponentType.ReadOnly<Created>(),
				ComponentType.ReadOnly<Game.Objects.Object>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
				ComponentType.ReadOnly<Game.Routes.TransportStop>()
			},
			None = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Temp>()
			}
		}, new EntityQueryDesc
		{
			All = new ComponentType[2]
			{
				ComponentType.ReadOnly<Deleted>(),
				ComponentType.ReadOnly<Game.Objects.Object>()
			},
			Any = new ComponentType[3]
			{
				ComponentType.ReadOnly<Building>(),
				ComponentType.ReadOnly<Game.Buildings.ServiceUpgrade>(),
				ComponentType.ReadOnly<Game.Routes.TransportStop>()
			},
			None = new ComponentType[4]
			{
				ComponentType.ReadOnly<Abandoned>(),
				ComponentType.ReadOnly<Condemned>(),
				ComponentType.ReadOnly<Destroyed>(),
				ComponentType.ReadOnly<Temp>()
			}
		});
		m_TargetQuery = GetEntityQuery(ComponentType.ReadOnly<Building>(), ComponentType.Exclude<Game.Buildings.ServiceUpgrade>(), ComponentType.Exclude<Abandoned>(), ComponentType.Exclude<Condemned>(), ComponentType.Exclude<Destroyed>(), ComponentType.Exclude<Deleted>(), ComponentType.Exclude<Updated>(), ComponentType.Exclude<Temp>());
		m_EventQuery = GetEntityQuery(ComponentType.ReadOnly<Event>(), ComponentType.ReadOnly<CoverageUpdated>());
	}

	[Preserve]
	protected override void OnDestroy()
	{
		m_RecentDeps.Complete();
		m_RecentMap.Dispose();
		base.OnDestroy();
	}

	public void PostDeserialize(Context context)
	{
		m_RecentDeps.Complete();
		m_RecentMap.Clear();
		for (int i = 0; i < m_PendingContainers.Count; i++)
		{
			Entity item = m_PendingContainers[i];
			m_PendingContainers.RemoveAtSwapBack(i--);
			m_FeedbackContainers.Add(item);
		}
	}

	protected override void OnGamePreload(Purpose purpose, GameMode mode)
	{
		base.OnGamePreload(purpose, mode);
		base.Enabled = mode.IsGame();
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_AppliedQuery.IsEmptyIgnoreFilter && !m_ConfigurationQuery.IsEmptyIgnoreFilter)
		{
			ProcessModifications();
		}
		if (m_PendingContainers.Count != 0 && !m_EventQuery.IsEmptyIgnoreFilter)
		{
			UpdatePending();
		}
	}

	private void ProcessModifications()
	{
		NativeArray<Entity> nativeArray = m_AppliedQuery.ToEntityArray(Allocator.TempJob);
		PathfindParameters pathfindParameters = new PathfindParameters
		{
			m_MaxSpeed = 111.111115f,
			m_WalkSpeed = 5.555556f,
			m_Weights = new PathfindWeights(1f, 1f, 1f, 1f),
			m_PathfindFlags = (PathfindFlags.Stable | PathfindFlags.IgnoreFlow),
			m_IgnoredRules = (RuleFlags.HasBlockage | RuleFlags.ForbidCombustionEngines | RuleFlags.ForbidTransitTraffic | RuleFlags.ForbidHeavyTraffic | RuleFlags.ForbidPrivateTraffic | RuleFlags.ForbidSlowTraffic | RuleFlags.AvoidBicycles)
		};
		SetupQueueTarget setupQueueTarget = default(SetupQueueTarget);
		while (m_FeedbackContainers.Count < nativeArray.Length)
		{
			m_FeedbackContainers.Add(base.EntityManager.CreateEntity(ComponentType.ReadWrite<Feedback>(), ComponentType.ReadWrite<ExtraFeedback>(), ComponentType.ReadWrite<CoverageElement>()));
		}
		m_TargetSeekerData.Update(this, m_AirwaySystem.GetAirwayData());
		SetupCoverageSearchJob jobData = new SetupCoverageSearchJob
		{
			m_BackSideData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Buildings_BackSide_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCoverageData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CoverageData_RO_ComponentLookup, ref base.CheckedStateRef)
		};
		FeedbackConfigurationData feedbackConfigurationData = default(FeedbackConfigurationData);
		IconCommandBuffer iconCommandBuffer = default(IconCommandBuffer);
		JobHandle jobHandle = default(JobHandle);
		bool flag = false;
		for (int i = 0; i < nativeArray.Length; i++)
		{
			Entity entity = nativeArray[i];
			Entity entity2 = entity;
			Owner component;
			while (base.EntityManager.TryGetComponent<Owner>(entity2, out component) && !base.EntityManager.HasComponent<CoverageServiceType>(entity2))
			{
				entity2 = component.m_Owner;
			}
			bool flag2 = base.EntityManager.HasComponent<Deleted>(entity);
			if (entity2 != entity)
			{
				if (flag2)
				{
					if (base.EntityManager.HasComponent<Deleted>(entity2))
					{
						continue;
					}
				}
				else if (base.EntityManager.HasComponent<Applied>(entity2))
				{
					continue;
				}
			}
			Transform componentData = base.EntityManager.GetComponentData<Transform>(entity2);
			PrefabRef componentData2 = base.EntityManager.GetComponentData<PrefabRef>(entity);
			PrefabRef componentData3 = base.EntityManager.GetComponentData<PrefabRef>(entity2);
			if (base.EntityManager.HasComponent<SpawnableBuildingData>(componentData2.m_Prefab))
			{
				if (flag2)
				{
					if (feedbackConfigurationData.m_HappyFaceNotification == Entity.Null)
					{
						feedbackConfigurationData = m_ConfigurationQuery.GetSingleton<FeedbackConfigurationData>();
						iconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer();
					}
					iconCommandBuffer.Add(entity, feedbackConfigurationData.m_SadFaceNotification, IconPriority.Info, IconClusterLayer.Transaction);
				}
				continue;
			}
			Entity entity3 = m_FeedbackContainers[m_FeedbackContainers.Count - 1];
			m_FeedbackContainers.RemoveAt(m_FeedbackContainers.Count - 1);
			m_PendingContainers.Add(entity3);
			base.EntityManager.SetComponentData(entity3, new Feedback
			{
				m_Position = componentData.m_Position,
				m_MainEntity = entity2,
				m_Prefab = componentData2.m_Prefab,
				m_MainPrefab = componentData3.m_Prefab,
				m_IsDeleted = flag2
			});
			DynamicBuffer<ExtraFeedback> buffer = base.EntityManager.GetBuffer<ExtraFeedback>(entity3);
			buffer.Clear();
			if (base.EntityManager.TryGetBuffer(entity2, isReadOnly: true, out DynamicBuffer<InstalledUpgrade> buffer2))
			{
				for (int j = 0; j < buffer2.Length; j++)
				{
					InstalledUpgrade installedUpgrade = buffer2[j];
					if (!BuildingUtils.CheckOption(installedUpgrade, BuildingOption.Inactive))
					{
						buffer.Add(new ExtraFeedback
						{
							m_Prefab = base.EntityManager.GetComponentData<PrefabRef>(installedUpgrade.m_Upgrade).m_Prefab
						});
					}
				}
			}
			base.EntityManager.GetBuffer<CoverageElement>(entity3).Clear();
			if (!base.EntityManager.TryGetSharedComponent<CoverageServiceType>(entity2, out var component2))
			{
				component2.m_Service = CoverageService.Count;
			}
			Game.Simulation.ServiceCoverageSystem.SetupPathfindMethods(component2.m_Service, ref pathfindParameters, ref setupQueueTarget);
			CoverageAction action = new CoverageAction(Allocator.Persistent);
			jobData.m_Entity = entity2;
			jobData.m_TargetSeeker = new PathfindTargetSeeker<PathfindTargetBuffer>(m_TargetSeekerData, pathfindParameters, setupQueueTarget, action.data.m_Sources.AsParallelWriter(), RandomSeed.Next(), isStartTarget: true);
			jobData.m_Action = action;
			JobHandle jobHandle2 = IJobExtensions.Schedule(jobData, base.Dependency);
			jobHandle = JobHandle.CombineDependencies(jobHandle, jobHandle2);
			m_PathfindQueueSystem.Enqueue(action, entity3, jobHandle2, uint.MaxValue, this, default(PathEventData), highPriority: true);
			flag = true;
		}
		nativeArray.Dispose();
		if (flag)
		{
			base.Dependency = jobHandle;
		}
	}

	private void UpdatePending()
	{
		NativeArray<CoverageUpdated> nativeArray = m_EventQuery.ToComponentDataArray<CoverageUpdated>(Allocator.TempJob);
		Entity singletonEntity = m_ConfigurationQuery.GetSingletonEntity();
		TargetCheckJob jobData = new TargetCheckJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_TransformType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Objects_Transform_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_Building_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GarbageProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_GarbageProducer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ElectricityConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_ElectricityConsumer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterConsumerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_WaterConsumer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MailProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_MailProducer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_CrimeProducerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Buildings_CrimeProducer_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrefabCoverageData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CoverageData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabGarbageFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_GarbageFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabHospitalData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_HospitalData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabDeathcareFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_DeathcareFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPowerPlantData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PowerPlantData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWindPoweredData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WindPoweredData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSolarPoweredData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SolarPoweredData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTransformerData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransformerData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWaterPumpingStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WaterPumpingStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSewageOutletData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabWastewaterTreatmentPlantData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_WastewaterTreatmentPlantData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTransportDepotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportDepotData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTransportStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPublicTransportStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PublicTransportStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabCargoTransportStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_CargoTransportStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTransportStopData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportStopData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPostFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PostFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabTelecomFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TelecomFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabSchoolData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_SchoolData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabParkingFacilityData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ParkingFacilityData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabMaintenanceDepotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_MaintenanceDepotData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabFireStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_FireStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPoliceStationData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PoliceStationData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPrisonData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PrisonData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabPollutionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_PollutionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PrefabAttractionData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_AttractionData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ServiceCoverages = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Net_ServiceCoverage_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabLocalModifierDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_LocalModifierData_RO_BufferLookup, ref base.CheckedStateRef),
			m_PrefabCityModifierDatas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_CityModifierData_RO_BufferLookup, ref base.CheckedStateRef),
			m_FeedbackConfigurationData = base.EntityManager.GetComponentData<FeedbackConfigurationData>(singletonEntity),
			m_FeedbackLocalEffectFactors = base.EntityManager.GetBuffer<FeedbackLocalEffectFactor>(singletonEntity, isReadOnly: true),
			m_FeedbackCityEffectFactors = base.EntityManager.GetBuffer<FeedbackCityEffectFactor>(singletonEntity, isReadOnly: true),
			m_RecentMap = m_RecentMap
		};
		NativeQueue<RecentUpdate> recentUpdates = default(NativeQueue<RecentUpdate>);
		JobHandle jobHandle = default(JobHandle);
		bool flag = false;
		for (int i = 0; i < m_PendingContainers.Count; i++)
		{
			Entity entity = m_PendingContainers[i];
			DynamicBuffer<CoverageElement> buffer = base.EntityManager.GetBuffer<CoverageElement>(entity, isReadOnly: true);
			if (buffer.Length != 0)
			{
				m_PendingContainers.RemoveAtSwapBack(i--);
				m_FeedbackContainers.Add(entity);
				NativeParallelHashMap<Entity, float2> coverageMap = new NativeParallelHashMap<Entity, float2>(buffer.Length, Allocator.TempJob);
				FillCoverageMapJob jobData2 = new FillCoverageMapJob
				{
					m_CoverageElements = buffer.AsNativeArray(),
					m_CoverageMap = coverageMap.AsParallelWriter()
				};
				jobData.m_FeedbackData = base.EntityManager.GetComponentData<Feedback>(entity);
				jobData.m_ExtraFeedbacks = base.EntityManager.GetBuffer<ExtraFeedback>(entity, isReadOnly: true);
				jobData.m_RandomSeed = RandomSeed.Next();
				jobData.m_CoverageMap = coverageMap;
				if (!flag)
				{
					recentUpdates = new NativeQueue<RecentUpdate>(Allocator.TempJob);
					jobData.m_TelecomCoverageData = m_TelecomCoverageSystem.GetData(readOnly: true, out var dependencies);
					jobData.m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer();
					jobData.m_RecentUpdates = recentUpdates.AsParallelWriter();
					jobHandle = JobHandle.CombineDependencies(base.Dependency, m_RecentDeps, dependencies);
				}
				JobHandle job = IJobParallelForExtensions.Schedule(jobData2, buffer.Length, 4);
				JobHandle jobHandle2 = JobChunkExtensions.ScheduleParallel(jobData, m_TargetQuery, JobHandle.CombineDependencies(jobHandle, job));
				coverageMap.Dispose(jobHandle2);
				jobHandle = jobHandle2;
				flag = true;
			}
		}
		for (int j = 0; j < nativeArray.Length; j++)
		{
			CoverageUpdated coverageUpdated = nativeArray[j];
			for (int k = 0; k < m_PendingContainers.Count; k++)
			{
				Entity entity2 = m_PendingContainers[k];
				if (entity2 == coverageUpdated.m_Owner)
				{
					m_PendingContainers.RemoveAtSwapBack(k--);
					m_FeedbackContainers.Add(entity2);
					jobData.m_FeedbackData = base.EntityManager.GetComponentData<Feedback>(entity2);
					jobData.m_ExtraFeedbacks = base.EntityManager.GetBuffer<ExtraFeedback>(entity2, isReadOnly: true);
					jobData.m_RandomSeed = RandomSeed.Next();
					jobData.m_CoverageMap = default(NativeParallelHashMap<Entity, float2>);
					if (!flag)
					{
						recentUpdates = new NativeQueue<RecentUpdate>(Allocator.TempJob);
						jobData.m_TelecomCoverageData = m_TelecomCoverageSystem.GetData(readOnly: true, out var dependencies2);
						jobData.m_IconCommandBuffer = m_IconCommandSystem.CreateCommandBuffer();
						jobData.m_RecentUpdates = recentUpdates.AsParallelWriter();
						jobHandle = JobHandle.CombineDependencies(base.Dependency, m_RecentDeps, dependencies2);
					}
					jobHandle = JobChunkExtensions.ScheduleParallel(jobData, m_TargetQuery, jobHandle);
					flag = true;
					break;
				}
			}
		}
		nativeArray.Dispose();
		if (flag)
		{
			m_IconCommandSystem.AddCommandBufferWriter(jobHandle);
			m_TelecomCoverageSystem.AddReader(jobHandle);
			base.Dependency = jobHandle;
			UpdateRecentMapJob jobData3 = new UpdateRecentMapJob
			{
				m_RecentMap = m_RecentMap,
				m_RecentUpdates = recentUpdates,
				m_SimulationFrame = m_SimulationSystem.frameIndex
			};
			m_RecentDeps = IJobExtensions.Schedule(jobData3, jobHandle);
			recentUpdates.Dispose(m_RecentDeps);
		}
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
	public ToolFeedbackSystem()
	{
	}
}
