using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Colossal.Collections;
using Game.Areas;
using Game.Common;
using Game.Economy;
using Game.Net;
using Game.Rendering;
using Game.Routes;
using Game.Simulation;
using Game.Zones;
using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Internal;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine.Scripting;

namespace Game.Prefabs;

[CompilerGenerated]
public class InfoviewInitializeSystem : GameSystemBase
{
	private struct InfoModeData
	{
		public Entity m_Mode;

		public int m_Priority;
	}

	[BurstCompile]
	private struct FindInfoviewJob : IJobChunk
	{
		private struct InfoviewSearchData
		{
			public CoverageService m_CoverageService;

			public Game.Zones.AreaType m_AreaType;

			public bool m_IsOffice;

			public RoadTypes m_RoadTypes;

			public TransportType m_TransportType;

			public RouteType m_RouteType;

			public MapFeature m_MapFeature;

			public MaintenanceType m_MaintenanceType;

			public TerraformingTarget m_TerraformingTarget;

			public PollutionType m_PollutionTypes;

			public WaterType m_WaterTypes;

			public ulong m_BuildingTypes;

			public uint m_VehicleTypes;

			public uint m_NetStatusTypes;

			public int m_BuildingPriority;

			public int m_VehiclePriority;

			public int m_ZonePriority;

			public int m_TransportStopPriority;

			public int m_RoutePriority;

			public int m_CoveragePriority;

			public int m_ExtractorAreaPriority;

			public int m_MaintenanceDepotPriority;

			public int m_ParkingFacilityPriority;

			public int m_TerraformingPriority;

			public int m_WindPriority;

			public int m_PollutionPriority;

			public int m_WaterPriority;

			public int m_FlowPriority;

			public int m_NetStatusPriority;
		}

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_InfoviewChunks;

		[ReadOnly]
		public NativeList<ArchetypeChunk> m_InfomodeChunks;

		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<CoverageData> m_CoverageType;

		[ReadOnly]
		public ComponentTypeHandle<HospitalData> m_HospitalType;

		[ReadOnly]
		public ComponentTypeHandle<PowerPlantData> m_PowerPlantType;

		[ReadOnly]
		public ComponentTypeHandle<TransformerData> m_TransformerType;

		[ReadOnly]
		public ComponentTypeHandle<BatteryData> m_BatteryType;

		[ReadOnly]
		public ComponentTypeHandle<WaterPumpingStationData> m_WaterPumpingStationType;

		[ReadOnly]
		public ComponentTypeHandle<WaterTowerData> m_WaterTowerType;

		[ReadOnly]
		public ComponentTypeHandle<SewageOutletData> m_SewageOutletType;

		[ReadOnly]
		public ComponentTypeHandle<TransportDepotData> m_TransportDepotType;

		[ReadOnly]
		public ComponentTypeHandle<TransportStationData> m_TransportStationType;

		[ReadOnly]
		public ComponentTypeHandle<GarbageFacilityData> m_GarbageFacilityType;

		[ReadOnly]
		public ComponentTypeHandle<FireStationData> m_FireStationType;

		[ReadOnly]
		public ComponentTypeHandle<PoliceStationData> m_PoliceStationType;

		[ReadOnly]
		public ComponentTypeHandle<MaintenanceDepotData> m_MaintenanceDepotType;

		[ReadOnly]
		public ComponentTypeHandle<PostFacilityData> m_PostFacilityDataType;

		[ReadOnly]
		public ComponentTypeHandle<TelecomFacilityData> m_TelecomFacilityDataType;

		[ReadOnly]
		public ComponentTypeHandle<SchoolData> m_SchoolDataType;

		[ReadOnly]
		public ComponentTypeHandle<ParkData> m_ParkDataType;

		[ReadOnly]
		public ComponentTypeHandle<EmergencyShelterData> m_EmergencyShelterDataType;

		[ReadOnly]
		public ComponentTypeHandle<DisasterFacilityData> m_DisasterFacilityDataType;

		[ReadOnly]
		public ComponentTypeHandle<FirewatchTowerData> m_FirewatchTowerDataType;

		[ReadOnly]
		public ComponentTypeHandle<DeathcareFacilityData> m_DeathcareFacilityDataType;

		[ReadOnly]
		public ComponentTypeHandle<PrisonData> m_PrisonDataType;

		[ReadOnly]
		public ComponentTypeHandle<AdminBuildingData> m_AdminBuildingDataType;

		[ReadOnly]
		public ComponentTypeHandle<WelfareOfficeData> m_WelfareOfficeDataType;

		[ReadOnly]
		public ComponentTypeHandle<ResearchFacilityData> m_ResearchFacilityDataType;

		[ReadOnly]
		public ComponentTypeHandle<ParkingFacilityData> m_ParkingFacilityDataType;

		[ReadOnly]
		public ComponentTypeHandle<PowerLineData> m_PowerLineType;

		[ReadOnly]
		public ComponentTypeHandle<PipelineData> m_PipelineType;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityConnectionData> m_ElectricityConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeConnectionData> m_WaterPipeConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<ResourceConnectionData> m_ResourceConnectionType;

		[ReadOnly]
		public ComponentTypeHandle<ZoneData> m_ZoneType;

		[ReadOnly]
		public ComponentTypeHandle<TransportStopData> m_TransportStopType;

		[ReadOnly]
		public ComponentTypeHandle<WorkStopData> m_WorkStopType;

		[ReadOnly]
		public ComponentTypeHandle<RouteData> m_RouteType;

		[ReadOnly]
		public ComponentTypeHandle<TransportLineData> m_TransportLineType;

		[ReadOnly]
		public ComponentTypeHandle<ExtractorAreaData> m_ExtractorAreaType;

		[ReadOnly]
		public ComponentTypeHandle<TerraformingData> m_TerraformingType;

		[ReadOnly]
		public ComponentTypeHandle<WindPoweredData> m_WindPoweredType;

		[ReadOnly]
		public ComponentTypeHandle<WaterPoweredData> m_WaterPoweredType;

		[ReadOnly]
		public ComponentTypeHandle<GroundWaterPoweredData> m_GroundWaterPoweredType;

		[ReadOnly]
		public ComponentTypeHandle<PollutionData> m_PollutionType;

		[ReadOnly]
		public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<BuildingPropertyData> m_BuildingPropertyType;

		[ReadOnly]
		public ComponentTypeHandle<ServiceUpgradeData> m_ServiceUpgradeType;

		[ReadOnly]
		public BufferTypeHandle<InfoviewMode> m_InfoviewModeType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewCoverageData> m_InfoviewCoverageType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewAvailabilityData> m_InfoviewAvailabilityType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewBuildingData> m_InfoviewBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewVehicleData> m_InfoviewVehicleType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewTransportStopData> m_InfoviewTransportStopType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewRouteData> m_InfoviewRouteType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewHeatmapData> m_InfoviewHeatmapType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewObjectStatusData> m_InfoviewObjectStatusType;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewNetStatusData> m_InfoviewNetStatusType;

		public BufferTypeHandle<PlaceableInfoviewItem> m_PlaceableInfoviewType;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			InfoviewSearchData searchData = default(InfoviewSearchData);
			bool flag = false;
			bool flag2 = false;
			bool flag3 = false;
			bool num = chunk.Has(ref m_SpawnableBuildingType);
			bool flag4 = chunk.Has(ref m_ServiceUpgradeType);
			bool flag5 = chunk.Has(ref m_CoverageType);
			if ((!num && !flag4) || flag5)
			{
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.Hospital, chunk, m_HospitalType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.PowerPlant, chunk, m_PowerPlantType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.Transformer, chunk, m_TransformerType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.Battery, chunk, m_BatteryType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.FreshWaterBuilding, chunk, m_WaterPumpingStationType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.FreshWaterBuilding, chunk, m_WaterTowerType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.SewageBuilding, chunk, m_SewageOutletType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.TransportDepot, chunk, m_TransportDepotType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.TransportStation, chunk, m_TransportStationType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.GarbageFacility, chunk, m_GarbageFacilityType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.FireStation, chunk, m_FireStationType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.PoliceStation, chunk, m_PoliceStationType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.PostFacility, chunk, m_PostFacilityDataType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.TelecomFacility, chunk, m_TelecomFacilityDataType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.School, chunk, m_SchoolDataType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.Park, chunk, m_ParkDataType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.EmergencyShelter, chunk, m_EmergencyShelterDataType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.DisasterFacility, chunk, m_DisasterFacilityDataType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.FirewatchTower, chunk, m_FirewatchTowerDataType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.DeathcareFacility, chunk, m_DeathcareFacilityDataType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.Prison, chunk, m_PrisonDataType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.AdminBuilding, chunk, m_AdminBuildingDataType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.WelfareOffice, chunk, m_WelfareOfficeDataType);
				CheckBuildingType(ref searchData.m_BuildingTypes, ref searchData.m_BuildingPriority, BuildingType.ResearchFacility, chunk, m_ResearchFacilityDataType);
				if ((searchData.m_BuildingTypes & 0x4000006) != 0L)
				{
					searchData.m_NetStatusPriority = 100;
					searchData.m_NetStatusTypes = 96u;
				}
				if ((searchData.m_BuildingTypes & 0x18) != 0L)
				{
					searchData.m_WaterPriority = 10000;
					flag = true;
				}
				if ((searchData.m_BuildingTypes & 8) != 0L)
				{
					searchData.m_NetStatusPriority = 100;
					searchData.m_NetStatusTypes = 128u;
				}
				if ((searchData.m_BuildingTypes & 0x10) != 0L)
				{
					searchData.m_NetStatusPriority = 100;
					searchData.m_NetStatusTypes = 256u;
				}
				if ((searchData.m_BuildingTypes & 0x80) != 0L)
				{
					searchData.m_RouteType = RouteType.TransportLine;
					searchData.m_RoutePriority = 10000;
				}
				if ((searchData.m_BuildingTypes & 0x100) != 0L)
				{
					searchData.m_VehicleTypes |= 256u;
					searchData.m_VehiclePriority = 10000;
				}
				if (chunk.Has(ref m_WorkStopType))
				{
					searchData.m_RoutePriority = 10000;
					flag = true;
				}
				else if (chunk.Has(ref m_TransportStopType))
				{
					searchData.m_TransportStopPriority = 1000000;
					searchData.m_RoutePriority = 10000;
					flag = true;
				}
				if (chunk.Has(ref m_MaintenanceDepotType))
				{
					searchData.m_MaintenanceDepotPriority = 1000000;
					flag = true;
				}
				if (chunk.Has(ref m_ParkingFacilityDataType))
				{
					searchData.m_ParkingFacilityPriority = 1000000;
					flag = true;
				}
				if (chunk.Has(ref m_TerraformingType))
				{
					searchData.m_TerraformingPriority = 1000000;
					flag = true;
				}
				if (flag5)
				{
					searchData.m_CoverageService = chunk.GetNativeArray(ref m_CoverageType)[0].m_Service;
					searchData.m_CoveragePriority = 10000;
				}
				if (chunk.Has(ref m_WindPoweredType))
				{
					searchData.m_WindPriority = 10000;
				}
				if (chunk.Has(ref m_WaterPoweredType))
				{
					searchData.m_WaterPriority = 10000;
					searchData.m_WaterTypes |= WaterType.Flowing;
				}
				if (chunk.Has(ref m_GroundWaterPoweredType))
				{
					searchData.m_WaterPriority = 10000;
					searchData.m_WaterTypes |= WaterType.Ground;
				}
				if (chunk.Has(ref m_ExtractorAreaType))
				{
					searchData.m_ExtractorAreaPriority = 10000;
					flag = true;
				}
				else if (chunk.Has(ref m_BuildingPropertyType))
				{
					flag = true;
				}
				if (chunk.Has(ref m_ZoneType))
				{
					searchData.m_ZonePriority = 1000000;
					searchData.m_PollutionPriority = 10000;
					flag = true;
				}
				else if (chunk.Has(ref m_PollutionType))
				{
					searchData.m_PollutionPriority = 100;
					flag = true;
				}
			}
			if (!num)
			{
				if (chunk.Has(ref m_PowerLineType))
				{
					searchData.m_NetStatusPriority = 1000000;
					flag = true;
					flag2 = true;
				}
				if (chunk.Has(ref m_PipelineType))
				{
					searchData.m_NetStatusPriority = 1000000;
					flag = true;
					flag3 = true;
				}
				if (chunk.Has(ref m_RouteType))
				{
					searchData.m_RoutePriority = 1000000;
					searchData.m_TransportStopPriority = 10000;
					flag = true;
				}
			}
			BufferAccessor<PlaceableInfoviewItem> bufferAccessor = chunk.GetBufferAccessor(ref m_PlaceableInfoviewType);
			NativeParallelHashMap<Entity, int> infomodeScores = new NativeParallelHashMap<Entity, int>(100, Allocator.Temp);
			NativeList<InfoModeData> supplementalModes = new NativeList<InfoModeData>(10, Allocator.Temp);
			if (flag)
			{
				NativeArray<ZoneData> nativeArray = chunk.GetNativeArray(ref m_ZoneType);
				NativeArray<TransportStopData> nativeArray2 = chunk.GetNativeArray(ref m_TransportStopType);
				NativeArray<WorkStopData> nativeArray3 = chunk.GetNativeArray(ref m_WorkStopType);
				NativeArray<RouteData> nativeArray4 = chunk.GetNativeArray(ref m_RouteType);
				NativeArray<TransportLineData> nativeArray5 = chunk.GetNativeArray(ref m_TransportLineType);
				NativeArray<ExtractorAreaData> nativeArray6 = chunk.GetNativeArray(ref m_ExtractorAreaType);
				NativeArray<BuildingPropertyData> nativeArray7 = chunk.GetNativeArray(ref m_BuildingPropertyType);
				NativeArray<MaintenanceDepotData> nativeArray8 = chunk.GetNativeArray(ref m_MaintenanceDepotType);
				NativeArray<ParkingFacilityData> nativeArray9 = chunk.GetNativeArray(ref m_ParkingFacilityDataType);
				NativeArray<TerraformingData> nativeArray10 = chunk.GetNativeArray(ref m_TerraformingType);
				NativeArray<PollutionData> nativeArray11 = chunk.GetNativeArray(ref m_PollutionType);
				NativeArray<WaterPumpingStationData> nativeArray12 = chunk.GetNativeArray(ref m_WaterPumpingStationType);
				NativeArray<SewageOutletData> nativeArray13 = chunk.GetNativeArray(ref m_SewageOutletType);
				NativeArray<ElectricityConnectionData> nativeArray14 = chunk.GetNativeArray(ref m_ElectricityConnectionType);
				NativeArray<WaterPipeConnectionData> nativeArray15 = chunk.GetNativeArray(ref m_WaterPipeConnectionType);
				NativeArray<ResourceConnectionData> nativeArray16 = chunk.GetNativeArray(ref m_ResourceConnectionType);
				bool flag6 = chunk.Has(ref m_WaterPoweredType);
				for (int i = 0; i < bufferAccessor.Length; i++)
				{
					if (searchData.m_ZonePriority != 0 && nativeArray.Length != 0)
					{
						ZoneData zoneData = nativeArray[i];
						searchData.m_AreaType = zoneData.m_AreaType;
						searchData.m_IsOffice = (zoneData.m_ZoneFlags & ZoneFlags.Office) != 0;
					}
					if (searchData.m_TransportStopPriority != 0)
					{
						if (nativeArray2.Length != 0)
						{
							searchData.m_TransportType = nativeArray2[i].m_TransportType;
						}
						else if (nativeArray5.Length != 0)
						{
							searchData.m_TransportType = nativeArray5[i].m_TransportType;
						}
						else if (nativeArray4.Length != 0)
						{
							searchData.m_TransportType = ((nativeArray4[i].m_Type == RouteType.WorkRoute) ? TransportType.Work : TransportType.None);
						}
					}
					if (searchData.m_RoutePriority != 0)
					{
						if (nativeArray3.Length != 0)
						{
							searchData.m_RouteType = RouteType.WorkRoute;
						}
						else if (nativeArray2.Length != 0)
						{
							searchData.m_RouteType = RouteType.TransportLine;
						}
						else if (nativeArray4.Length != 0)
						{
							searchData.m_RouteType = nativeArray4[i].m_Type;
						}
					}
					if (searchData.m_ExtractorAreaPriority != 0 && nativeArray6.Length != 0)
					{
						ExtractorAreaData extractorAreaData = nativeArray6[i];
						if (extractorAreaData.m_RequireNaturalResource)
						{
							searchData.m_MapFeature = extractorAreaData.m_MapFeature;
						}
					}
					if (nativeArray7.Length != 0 && EconomyUtils.IsExtractorResource(nativeArray7[i].m_AllowedManufactured))
					{
						searchData.m_BuildingPriority = 1000000;
						searchData.m_BuildingTypes |= 4294967296uL;
					}
					if (searchData.m_MaintenanceDepotPriority != 0 && nativeArray8.Length != 0)
					{
						searchData.m_MaintenanceType = nativeArray8[i].m_MaintenanceType;
					}
					if (searchData.m_ParkingFacilityPriority != 0 && nativeArray9.Length != 0)
					{
						searchData.m_RoadTypes = nativeArray9[i].m_RoadTypes;
					}
					if (searchData.m_TerraformingPriority != 0 && nativeArray10.Length != 0)
					{
						searchData.m_TerraformingTarget = nativeArray10[i].m_Target;
					}
					if (searchData.m_PollutionPriority != 0)
					{
						if (nativeArray.Length != 0)
						{
							ZoneData zoneData2 = nativeArray[i];
							if (zoneData2.m_AreaType == Game.Zones.AreaType.Residential)
							{
								searchData.m_PollutionTypes |= PollutionType.Ground;
							}
							if (zoneData2.m_AreaType == Game.Zones.AreaType.Industrial)
							{
								searchData.m_PollutionTypes |= PollutionType.Ground | PollutionType.Air;
							}
						}
						else if (nativeArray11.Length != 0)
						{
							PollutionData pollutionData = nativeArray11[i];
							searchData.m_PollutionTypes = PollutionType.None;
							if (pollutionData.m_GroundPollution > 0f)
							{
								searchData.m_PollutionTypes |= PollutionType.Ground;
							}
							if (pollutionData.m_AirPollution > 0f)
							{
								searchData.m_PollutionTypes |= PollutionType.Air;
							}
							if (pollutionData.m_NoisePollution > 0f)
							{
								searchData.m_PollutionTypes |= PollutionType.Noise;
							}
						}
					}
					if (searchData.m_WaterPriority != 0 && (nativeArray12.Length != 0 || nativeArray13.Length != 0 || flag6))
					{
						searchData.m_WaterTypes = WaterType.None;
						if (nativeArray12.Length != 0)
						{
							WaterPumpingStationData waterPumpingStationData = nativeArray12[i];
							if ((waterPumpingStationData.m_Types & AllowedWaterTypes.Groundwater) != AllowedWaterTypes.None)
							{
								searchData.m_WaterTypes |= WaterType.Ground;
							}
							if ((waterPumpingStationData.m_Types & AllowedWaterTypes.SurfaceWater) != AllowedWaterTypes.None)
							{
								searchData.m_WaterTypes |= WaterType.Flowing;
							}
						}
						if (nativeArray13.Length != 0)
						{
							searchData.m_WaterTypes |= WaterType.Flowing;
						}
						if (flag6)
						{
							searchData.m_WaterTypes |= WaterType.Flowing;
						}
					}
					if (searchData.m_NetStatusPriority != 0)
					{
						if (nativeArray14.Length != 0 && flag2)
						{
							switch (nativeArray14[i].m_Voltage)
							{
							case ElectricityConnection.Voltage.Low:
								searchData.m_NetStatusTypes |= 32u;
								break;
							case ElectricityConnection.Voltage.High:
								searchData.m_NetStatusTypes |= 64u;
								break;
							}
						}
						if (nativeArray15.Length != 0 && flag3)
						{
							WaterPipeConnectionData waterPipeConnectionData = nativeArray15[i];
							if (waterPipeConnectionData.m_FreshCapacity != 0)
							{
								searchData.m_NetStatusTypes |= 128u;
							}
							if (waterPipeConnectionData.m_SewageCapacity != 0)
							{
								searchData.m_NetStatusTypes |= 256u;
							}
						}
						if (nativeArray16.Length != 0 && flag3 && nativeArray16[i].m_Resource == Resource.Oil)
						{
							searchData.m_NetStatusTypes |= 512u;
						}
					}
					CalculateInfomodeScores(searchData, infomodeScores);
					supplementalModes.Clear();
					int bestScore;
					Entity bestInfoView = GetBestInfoView(searchData, infomodeScores, supplementalModes, out bestScore);
					DynamicBuffer<PlaceableInfoviewItem> dynamicBuffer = bufferAccessor[i];
					dynamicBuffer.Clear();
					if (bestInfoView != Entity.Null)
					{
						dynamicBuffer.Capacity = 1 + supplementalModes.Length;
						dynamicBuffer.Add(new PlaceableInfoviewItem
						{
							m_Item = bestInfoView,
							m_Priority = bestScore
						});
						for (int j = 0; j < supplementalModes.Length; j++)
						{
							InfoModeData infoModeData = supplementalModes[j];
							dynamicBuffer.Add(new PlaceableInfoviewItem
							{
								m_Item = infoModeData.m_Mode,
								m_Priority = infoModeData.m_Priority
							});
						}
					}
				}
			}
			else
			{
				CalculateInfomodeScores(searchData, infomodeScores);
				supplementalModes.Clear();
				int bestScore2;
				Entity bestInfoView2 = GetBestInfoView(searchData, infomodeScores, supplementalModes, out bestScore2);
				for (int k = 0; k < bufferAccessor.Length; k++)
				{
					DynamicBuffer<PlaceableInfoviewItem> dynamicBuffer2 = bufferAccessor[k];
					dynamicBuffer2.Clear();
					if (bestInfoView2 != Entity.Null)
					{
						dynamicBuffer2.Capacity = 1 + supplementalModes.Length;
						dynamicBuffer2.Add(new PlaceableInfoviewItem
						{
							m_Item = bestInfoView2,
							m_Priority = bestScore2
						});
						for (int l = 0; l < supplementalModes.Length; l++)
						{
							InfoModeData infoModeData2 = supplementalModes[l];
							dynamicBuffer2.Add(new PlaceableInfoviewItem
							{
								m_Item = infoModeData2.m_Mode,
								m_Priority = infoModeData2.m_Priority
							});
						}
					}
				}
			}
			infomodeScores.Dispose();
			supplementalModes.Dispose();
		}

		private void CheckBuildingType<T>(ref ulong mask, ref int priority, BuildingType type, ArchetypeChunk chunk, ComponentTypeHandle<T> componentType) where T : struct, IComponentData
		{
			bool test = chunk.Has(ref componentType);
			mask = math.select(mask, mask | (ulong)(1L << (int)type), test);
			priority = math.select(priority, 1000000, test);
		}

		private void CalculateInfomodeScores(InfoviewSearchData searchData, NativeParallelHashMap<Entity, int> infomodeScores)
		{
			infomodeScores.Clear();
			for (int i = 0; i < m_InfomodeChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_InfomodeChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				if (searchData.m_BuildingPriority != 0)
				{
					NativeArray<InfoviewBuildingData> nativeArray2 = archetypeChunk.GetNativeArray(ref m_InfoviewBuildingType);
					for (int j = 0; j < nativeArray2.Length; j++)
					{
						InfoviewBuildingData infoviewBuildingData = nativeArray2[j];
						int score = math.select(-1, searchData.m_BuildingPriority, (searchData.m_BuildingTypes & (ulong)(1L << (int)infoviewBuildingData.m_Type)) != 0);
						AddInfomodeScore(infomodeScores, nativeArray[j], score);
					}
				}
				if (searchData.m_MaintenanceDepotPriority != 0)
				{
					NativeArray<InfoviewBuildingData> nativeArray3 = archetypeChunk.GetNativeArray(ref m_InfoviewBuildingType);
					for (int k = 0; k < nativeArray3.Length; k++)
					{
						InfoviewBuildingData infoviewBuildingData2 = nativeArray3[k];
						int score2 = math.select(-1, searchData.m_MaintenanceDepotPriority, (searchData.m_MaintenanceType & GetMaintenanceType(infoviewBuildingData2.m_Type)) != 0);
						AddInfomodeScore(infomodeScores, nativeArray[k], score2);
					}
				}
				if (searchData.m_ParkingFacilityPriority != 0)
				{
					NativeArray<InfoviewBuildingData> nativeArray4 = archetypeChunk.GetNativeArray(ref m_InfoviewBuildingType);
					for (int l = 0; l < nativeArray4.Length; l++)
					{
						InfoviewBuildingData infoviewBuildingData3 = nativeArray4[l];
						int score3 = math.select(-1, searchData.m_ParkingFacilityPriority, (searchData.m_RoadTypes & GetRoadType(infoviewBuildingData3.m_Type)) != 0);
						AddInfomodeScore(infomodeScores, nativeArray[l], score3);
					}
				}
				if (searchData.m_ZonePriority != 0)
				{
					NativeArray<InfoviewAvailabilityData> nativeArray5 = archetypeChunk.GetNativeArray(ref m_InfoviewAvailabilityType);
					for (int m = 0; m < nativeArray5.Length; m++)
					{
						InfoviewAvailabilityData infoviewAvailabilityData = nativeArray5[m];
						int score4 = math.select(-1, searchData.m_ZonePriority, searchData.m_AreaType == infoviewAvailabilityData.m_AreaType && searchData.m_IsOffice == infoviewAvailabilityData.m_Office);
						AddInfomodeScore(infomodeScores, nativeArray[m], score4);
					}
				}
				if (searchData.m_TransportStopPriority != 0)
				{
					NativeArray<InfoviewTransportStopData> nativeArray6 = archetypeChunk.GetNativeArray(ref m_InfoviewTransportStopType);
					for (int n = 0; n < nativeArray6.Length; n++)
					{
						InfoviewTransportStopData infoviewTransportStopData = nativeArray6[n];
						int score5 = math.select(-1, searchData.m_TransportStopPriority, searchData.m_TransportType == infoviewTransportStopData.m_Type);
						AddInfomodeScore(infomodeScores, nativeArray[n], score5);
					}
				}
				if (searchData.m_RoutePriority != 0)
				{
					NativeArray<InfoviewRouteData> nativeArray7 = archetypeChunk.GetNativeArray(ref m_InfoviewRouteType);
					for (int num = 0; num < nativeArray7.Length; num++)
					{
						InfoviewRouteData infoviewRouteData = nativeArray7[num];
						int score6 = math.select(-1, searchData.m_RoutePriority, searchData.m_RouteType == infoviewRouteData.m_Type);
						AddInfomodeScore(infomodeScores, nativeArray[num], score6);
					}
				}
				if (searchData.m_TerraformingPriority != 0)
				{
					NativeArray<InfoviewHeatmapData> nativeArray8 = archetypeChunk.GetNativeArray(ref m_InfoviewHeatmapType);
					for (int num2 = 0; num2 < nativeArray8.Length; num2++)
					{
						InfoviewHeatmapData infoviewHeatmapData = nativeArray8[num2];
						int score7 = math.select(-1, searchData.m_TerraformingPriority, searchData.m_TerraformingTarget == GetTerraformingTarget(infoviewHeatmapData.m_Type));
						AddInfomodeScore(infomodeScores, nativeArray[num2], score7);
					}
				}
				if (searchData.m_VehiclePriority != 0)
				{
					NativeArray<InfoviewVehicleData> nativeArray9 = archetypeChunk.GetNativeArray(ref m_InfoviewVehicleType);
					for (int num3 = 0; num3 < nativeArray9.Length; num3++)
					{
						InfoviewVehicleData infoviewVehicleData = nativeArray9[num3];
						int score8 = math.select(-1, searchData.m_VehiclePriority, (searchData.m_VehicleTypes & (uint)(1 << (int)infoviewVehicleData.m_Type)) != 0);
						AddInfomodeScore(infomodeScores, nativeArray[num3], score8);
					}
				}
				if (searchData.m_CoveragePriority != 0)
				{
					NativeArray<InfoviewCoverageData> nativeArray10 = archetypeChunk.GetNativeArray(ref m_InfoviewCoverageType);
					for (int num4 = 0; num4 < nativeArray10.Length; num4++)
					{
						InfoviewCoverageData infoviewCoverageData = nativeArray10[num4];
						int score9 = math.select(-1, searchData.m_CoveragePriority, searchData.m_CoverageService == infoviewCoverageData.m_Service);
						AddInfomodeScore(infomodeScores, nativeArray[num4], score9);
					}
				}
				if (searchData.m_WaterPriority != 0)
				{
					NativeArray<InfoviewHeatmapData> nativeArray11 = archetypeChunk.GetNativeArray(ref m_InfoviewHeatmapType);
					for (int num5 = 0; num5 < nativeArray11.Length; num5++)
					{
						InfoviewHeatmapData infoviewHeatmapData2 = nativeArray11[num5];
						int score10 = math.select(-1, searchData.m_WaterPriority, (searchData.m_WaterTypes & GetWaterType(infoviewHeatmapData2.m_Type)) != 0);
						AddInfomodeScore(infomodeScores, nativeArray[num5], score10);
					}
				}
				if (searchData.m_WindPriority != 0)
				{
					NativeArray<InfoviewHeatmapData> nativeArray12 = archetypeChunk.GetNativeArray(ref m_InfoviewHeatmapType);
					for (int num6 = 0; num6 < nativeArray12.Length; num6++)
					{
						InfoviewHeatmapData infoviewHeatmapData3 = nativeArray12[num6];
						int score11 = math.select(-1, searchData.m_WindPriority, infoviewHeatmapData3.m_Type == HeatmapData.Wind);
						AddInfomodeScore(infomodeScores, nativeArray[num6], score11);
					}
				}
				if (searchData.m_ExtractorAreaPriority != 0)
				{
					NativeArray<InfoviewHeatmapData> nativeArray13 = archetypeChunk.GetNativeArray(ref m_InfoviewHeatmapType);
					NativeArray<InfoviewObjectStatusData> nativeArray14 = archetypeChunk.GetNativeArray(ref m_InfoviewObjectStatusType);
					for (int num7 = 0; num7 < nativeArray13.Length; num7++)
					{
						InfoviewHeatmapData infoviewHeatmapData4 = nativeArray13[num7];
						int score12 = math.select(-1, searchData.m_ExtractorAreaPriority, searchData.m_MapFeature == GetMapFeature(infoviewHeatmapData4.m_Type));
						AddInfomodeScore(infomodeScores, nativeArray[num7], score12);
					}
					for (int num8 = 0; num8 < nativeArray14.Length; num8++)
					{
						InfoviewObjectStatusData infoviewObjectStatusData = nativeArray14[num8];
						int score13 = math.select(-1, searchData.m_ExtractorAreaPriority, searchData.m_MapFeature == GetMapFeature(infoviewObjectStatusData.m_Type));
						AddInfomodeScore(infomodeScores, nativeArray[num8], score13);
					}
				}
				if (searchData.m_PollutionPriority != 0)
				{
					NativeArray<InfoviewHeatmapData> nativeArray15 = archetypeChunk.GetNativeArray(ref m_InfoviewHeatmapType);
					for (int num9 = 0; num9 < nativeArray15.Length; num9++)
					{
						InfoviewHeatmapData infoviewHeatmapData5 = nativeArray15[num9];
						int score14 = math.select(-1, searchData.m_PollutionPriority, (searchData.m_PollutionTypes & GetPollutionType(infoviewHeatmapData5.m_Type)) != 0);
						AddInfomodeScore(infomodeScores, nativeArray[num9], score14);
					}
				}
				if (searchData.m_NetStatusPriority != 0)
				{
					NativeArray<InfoviewNetStatusData> nativeArray16 = archetypeChunk.GetNativeArray(ref m_InfoviewNetStatusType);
					for (int num10 = 0; num10 < nativeArray16.Length; num10++)
					{
						InfoviewNetStatusData infoviewNetStatusData = nativeArray16[num10];
						int score15 = math.select(-1, searchData.m_NetStatusPriority, (searchData.m_NetStatusTypes & (uint)(1 << (int)infoviewNetStatusData.m_Type)) != 0);
						AddInfomodeScore(infomodeScores, nativeArray[num10], score15);
					}
				}
			}
		}

		private void AddInfomodeScore(NativeParallelHashMap<Entity, int> infomodeScores, Entity entity, int score)
		{
			if (!infomodeScores.TryAdd(entity, score))
			{
				infomodeScores[entity] = math.max(infomodeScores[entity], score);
			}
		}

		private Entity GetBestInfoView(InfoviewSearchData searchData, NativeParallelHashMap<Entity, int> infomodeScores, NativeList<InfoModeData> supplementalModes, out int bestScore)
		{
			bestScore = int.MinValue;
			Entity result = Entity.Null;
			for (int i = 0; i < m_InfoviewChunks.Length; i++)
			{
				ArchetypeChunk archetypeChunk = m_InfoviewChunks[i];
				NativeArray<Entity> nativeArray = archetypeChunk.GetNativeArray(m_EntityType);
				BufferAccessor<InfoviewMode> bufferAccessor = archetypeChunk.GetBufferAccessor(ref m_InfoviewModeType);
				for (int j = 0; j < nativeArray.Length; j++)
				{
					DynamicBuffer<InfoviewMode> dynamicBuffer = bufferAccessor[j];
					int num = 0;
					bool flag = false;
					for (int k = 0; k < dynamicBuffer.Length; k++)
					{
						InfoviewMode infoviewMode = dynamicBuffer[k];
						if (infomodeScores.TryGetValue(infoviewMode.m_Mode, out var item))
						{
							bool flag2 = infoviewMode.m_Supplemental | infoviewMode.m_Optional;
							num += math.select(item, 0, flag2 && item < 0);
							flag = flag || (flag2 && item > 0);
						}
					}
					if (num <= bestScore && (bestScore != 0 || dynamicBuffer.Length != 0))
					{
						continue;
					}
					bestScore = num;
					result = nativeArray[j];
					supplementalModes.Clear();
					if (!flag)
					{
						continue;
					}
					for (int l = 0; l < dynamicBuffer.Length; l++)
					{
						InfoviewMode infoviewMode2 = dynamicBuffer[l];
						if ((infoviewMode2.m_Supplemental | infoviewMode2.m_Optional) && infomodeScores.TryGetValue(infoviewMode2.m_Mode, out var item2) && item2 > 0)
						{
							InfoModeData value = new InfoModeData
							{
								m_Mode = infoviewMode2.m_Mode,
								m_Priority = item2
							};
							supplementalModes.Add(in value);
						}
					}
				}
			}
			return result;
		}

		public static MapFeature GetMapFeature(HeatmapData heatmapType)
		{
			return heatmapType switch
			{
				HeatmapData.Fertility => MapFeature.FertileLand, 
				HeatmapData.Ore => MapFeature.Ore, 
				HeatmapData.Oil => MapFeature.Oil, 
				HeatmapData.Fish => MapFeature.Fish, 
				_ => MapFeature.None, 
			};
		}

		public static TerraformingTarget GetTerraformingTarget(HeatmapData heatmapType)
		{
			return heatmapType switch
			{
				HeatmapData.Fertility => TerraformingTarget.FertileLand, 
				HeatmapData.Ore => TerraformingTarget.Ore, 
				HeatmapData.Oil => TerraformingTarget.Oil, 
				HeatmapData.GroundWater => TerraformingTarget.GroundWater, 
				_ => TerraformingTarget.None, 
			};
		}

		public static MapFeature GetMapFeature(ObjectStatusType statusType)
		{
			if (statusType == ObjectStatusType.WoodResource)
			{
				return MapFeature.Forest;
			}
			return MapFeature.None;
		}

		public static MaintenanceType GetMaintenanceType(BuildingType buildingType)
		{
			return buildingType switch
			{
				BuildingType.RoadMaintenanceDepot => MaintenanceType.Road | MaintenanceType.Snow | MaintenanceType.Vehicle, 
				BuildingType.ParkMaintenanceDepot => MaintenanceType.Park, 
				_ => MaintenanceType.None, 
			};
		}

		public static RoadTypes GetRoadType(BuildingType buildingType)
		{
			return buildingType switch
			{
				BuildingType.CarParkingFacility => RoadTypes.Car, 
				BuildingType.BicycleParkingFacility => RoadTypes.Bicycle, 
				_ => RoadTypes.None, 
			};
		}

		public static PollutionType GetPollutionType(HeatmapData heatmapType)
		{
			return heatmapType switch
			{
				HeatmapData.GroundPollution => PollutionType.Ground, 
				HeatmapData.GroundWater => PollutionType.Ground, 
				HeatmapData.Wind => PollutionType.Air, 
				_ => PollutionType.None, 
			};
		}

		public static WaterType GetWaterType(HeatmapData heatmapType)
		{
			return heatmapType switch
			{
				HeatmapData.GroundWater => WaterType.Ground, 
				HeatmapData.GroundWaterPollution => WaterType.Ground, 
				HeatmapData.WaterFlow => WaterType.Flowing, 
				HeatmapData.WaterPollution => WaterType.Flowing, 
				_ => WaterType.None, 
			};
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[Flags]
	private enum PollutionType
	{
		None = 0,
		Ground = 1,
		Air = 2,
		Noise = 4
	}

	[Flags]
	private enum WaterType
	{
		None = 0,
		Ground = 1,
		Flowing = 2
	}

	private struct InfoviewBufferData
	{
		public Entity m_Target;

		public Entity m_Source;
	}

	[BurstCompile]
	private struct FindSubInfoviewJob : IJobChunk
	{
		[ReadOnly]
		public EntityTypeHandle m_EntityType;

		[ReadOnly]
		public ComponentTypeHandle<SpawnableBuildingData> m_SpawnableBuildingType;

		[ReadOnly]
		public ComponentTypeHandle<BuildingPropertyData> m_BuildingPropertyType;

		[ReadOnly]
		public ComponentTypeHandle<NetData> m_NetDataType;

		[ReadOnly]
		public BufferTypeHandle<PlaceableInfoviewItem> m_PlaceableInfoviewType;

		[ReadOnly]
		public BufferTypeHandle<SubArea> m_SubAreaType;

		[ReadOnly]
		public BufferTypeHandle<SubObject> m_SubObjectType;

		[ReadOnly]
		public BufferTypeHandle<BuildingUpgradeElement> m_BuildingUpgradeElementType;

		[ReadOnly]
		public ComponentLookup<LotData> m_LotData;

		[ReadOnly]
		public ComponentLookup<ResourceData> m_ResourceData;

		[ReadOnly]
		public ComponentLookup<TransportStopData> m_TransportStopData;

		[ReadOnly]
		public BufferLookup<PlaceableInfoviewItem> m_PlaceableInfoviewData;

		[ReadOnly]
		public BufferLookup<InfoviewMode> m_InfoviewModes;

		[ReadOnly]
		public BufferLookup<SubArea> m_SubAreas;

		[ReadOnly]
		public ResourcePrefabs m_ResourcePrefabs;

		public NativeQueue<InfoviewBufferData>.ParallelWriter m_InfoViewBuffer;

		public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			NativeArray<Entity> nativeArray = chunk.GetNativeArray(m_EntityType);
			NativeArray<SpawnableBuildingData> nativeArray2 = chunk.GetNativeArray(ref m_SpawnableBuildingType);
			NativeArray<BuildingPropertyData> nativeArray3 = chunk.GetNativeArray(ref m_BuildingPropertyType);
			BufferAccessor<PlaceableInfoviewItem> bufferAccessor = chunk.GetBufferAccessor(ref m_PlaceableInfoviewType);
			BufferAccessor<SubArea> bufferAccessor2 = chunk.GetBufferAccessor(ref m_SubAreaType);
			BufferAccessor<SubObject> bufferAccessor3 = chunk.GetBufferAccessor(ref m_SubObjectType);
			BufferAccessor<BuildingUpgradeElement> bufferAccessor4 = chunk.GetBufferAccessor(ref m_BuildingUpgradeElementType);
			bool flag = chunk.Has(ref m_NetDataType);
			for (int i = 0; i < bufferAccessor.Length; i++)
			{
				DynamicBuffer<PlaceableInfoviewItem> dynamicBuffer = bufferAccessor[i];
				int priority = int.MinValue;
				Entity sourceEntity = Entity.Null;
				if (dynamicBuffer.Length != 0)
				{
					PlaceableInfoviewItem placeableInfoviewItem = dynamicBuffer[0];
					if (m_InfoviewModes[placeableInfoviewItem.m_Item].Length != 0)
					{
						priority = placeableInfoviewItem.m_Priority;
					}
				}
				if (CollectionUtils.TryGet(nativeArray2, i, out var value) && m_PlaceableInfoviewData.TryGetBuffer(value.m_ZonePrefab, out var bufferData) && bufferData.Length != 0)
				{
					PlaceableInfoviewItem placeableInfoviewItem2 = bufferData[0];
					if (m_InfoviewModes[placeableInfoviewItem2.m_Item].Length != 0 && placeableInfoviewItem2.m_Priority > priority)
					{
						priority = placeableInfoviewItem2.m_Priority;
						sourceEntity = value.m_ZonePrefab;
					}
				}
				if (CollectionUtils.TryGet(nativeArray3, i, out var value2) && EconomyUtils.IsExtractorResource(value2.m_AllowedManufactured))
				{
					ResourceIterator resourceIterator = default(ResourceIterator);
					while (resourceIterator.Next())
					{
						if ((value2.m_AllowedManufactured & resourceIterator.resource) != Resource.NoResource && m_ResourceData.TryGetComponent(m_ResourcePrefabs[value2.m_AllowedManufactured], out var componentData) && componentData.m_RequireNaturalResource)
						{
							priority = math.min(priority, 9000);
							break;
						}
					}
				}
				if (CollectionUtils.TryGet(bufferAccessor2, i, out var value3))
				{
					CheckSubAreas(ref priority, ref sourceEntity, value3);
				}
				if (CollectionUtils.TryGet(bufferAccessor4, i, out var value4))
				{
					for (int j = 0; j < value4.Length; j++)
					{
						if (m_SubAreas.TryGetBuffer(value4[j].m_Upgrade, out value3))
						{
							CheckSubAreas(ref priority, ref sourceEntity, value3);
						}
					}
				}
				if (flag && CollectionUtils.TryGet(bufferAccessor3, i, out var value5))
				{
					for (int k = 0; k < value5.Length; k++)
					{
						SubObject subObject = value5[k];
						if (m_TransportStopData.HasComponent(subObject.m_Prefab) && m_PlaceableInfoviewData.TryGetBuffer(subObject.m_Prefab, out bufferData) && bufferData.Length != 0)
						{
							PlaceableInfoviewItem placeableInfoviewItem3 = bufferData[0];
							if (m_InfoviewModes[placeableInfoviewItem3.m_Item].Length != 0 && placeableInfoviewItem3.m_Priority > priority)
							{
								priority = placeableInfoviewItem3.m_Priority;
								sourceEntity = subObject.m_Prefab;
							}
						}
					}
				}
				if (sourceEntity != Entity.Null)
				{
					m_InfoViewBuffer.Enqueue(new InfoviewBufferData
					{
						m_Target = nativeArray[i],
						m_Source = sourceEntity
					});
				}
			}
		}

		private void CheckSubAreas(ref int priority, ref Entity sourceEntity, DynamicBuffer<SubArea> subAreas)
		{
			for (int i = 0; i < subAreas.Length; i++)
			{
				Entity prefab = subAreas[i].m_Prefab;
				if (m_LotData.HasComponent(prefab) && m_PlaceableInfoviewData.TryGetBuffer(prefab, out var bufferData) && bufferData.Length != 0)
				{
					PlaceableInfoviewItem placeableInfoviewItem = bufferData[0];
					if (m_InfoviewModes[placeableInfoviewItem.m_Item].Length != 0 && placeableInfoviewItem.m_Priority > priority)
					{
						priority = placeableInfoviewItem.m_Priority;
						sourceEntity = prefab;
					}
				}
			}
		}

		void IJobChunk.Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
		{
			Execute(in chunk, unfilteredChunkIndex, useEnabledMask, in chunkEnabledMask);
		}
	}

	[BurstCompile]
	private struct AssignInfoviewJob : IJob
	{
		public NativeQueue<InfoviewBufferData> m_InfoViewBuffer;

		public BufferLookup<PlaceableInfoviewItem> m_PlaceableInfoviewData;

		public void Execute()
		{
			InfoviewBufferData item;
			while (m_InfoViewBuffer.TryDequeue(out item))
			{
				DynamicBuffer<PlaceableInfoviewItem> dynamicBuffer = m_PlaceableInfoviewData[item.m_Target];
				DynamicBuffer<PlaceableInfoviewItem> v = m_PlaceableInfoviewData[item.m_Source];
				dynamicBuffer.CopyFrom(v);
			}
		}
	}

	private struct TypeHandle
	{
		[ReadOnly]
		public ComponentTypeHandle<PrefabData> __Game_Prefabs_PrefabData_RO_ComponentTypeHandle;

		[ReadOnly]
		public EntityTypeHandle __Unity_Entities_Entity_TypeHandle;

		public BufferTypeHandle<InfoviewMode> __Game_Prefabs_InfoviewMode_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<InfomodeGroup> __Game_Prefabs_InfomodeGroup_RO_ComponentLookup;

		[ReadOnly]
		public ComponentTypeHandle<CoverageData> __Game_Prefabs_CoverageData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<HospitalData> __Game_Prefabs_HospitalData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PowerPlantData> __Game_Prefabs_PowerPlantData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TransformerData> __Game_Prefabs_TransformerData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BatteryData> __Game_Prefabs_BatteryData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterPumpingStationData> __Game_Prefabs_WaterPumpingStationData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterTowerData> __Game_Prefabs_WaterTowerData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SewageOutletData> __Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TransportDepotData> __Game_Prefabs_TransportDepotData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TransportStationData> __Game_Prefabs_TransportStationData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<GarbageFacilityData> __Game_Prefabs_GarbageFacilityData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<FireStationData> __Game_Prefabs_FireStationData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PoliceStationData> __Game_Prefabs_PoliceStationData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<MaintenanceDepotData> __Game_Prefabs_MaintenanceDepotData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PostFacilityData> __Game_Prefabs_PostFacilityData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TelecomFacilityData> __Game_Prefabs_TelecomFacilityData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SchoolData> __Game_Prefabs_SchoolData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkData> __Game_Prefabs_ParkData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<EmergencyShelterData> __Game_Prefabs_EmergencyShelterData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<DisasterFacilityData> __Game_Prefabs_DisasterFacilityData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<FirewatchTowerData> __Game_Prefabs_FirewatchTowerData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<DeathcareFacilityData> __Game_Prefabs_DeathcareFacilityData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PrisonData> __Game_Prefabs_PrisonData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<AdminBuildingData> __Game_Prefabs_AdminBuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WelfareOfficeData> __Game_Prefabs_WelfareOfficeData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ResearchFacilityData> __Game_Prefabs_ResearchFacilityData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ParkingFacilityData> __Game_Prefabs_ParkingFacilityData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PowerLineData> __Game_Prefabs_PowerLineData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PipelineData> __Game_Prefabs_PipelineData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ElectricityConnectionData> __Game_Prefabs_ElectricityConnectionData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterPipeConnectionData> __Game_Prefabs_WaterPipeConnectionData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ResourceConnectionData> __Game_Prefabs_ResourceConnectionData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ZoneData> __Game_Prefabs_ZoneData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TransportStopData> __Game_Prefabs_TransportStopData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WorkStopData> __Game_Prefabs_WorkStopData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<RouteData> __Game_Prefabs_RouteData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TransportLineData> __Game_Prefabs_TransportLineData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ExtractorAreaData> __Game_Prefabs_ExtractorAreaData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<TerraformingData> __Game_Prefabs_TerraformingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WindPoweredData> __Game_Prefabs_WindPoweredData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<WaterPoweredData> __Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<GroundWaterPoweredData> __Game_Prefabs_GroundWaterPoweredData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<PollutionData> __Game_Prefabs_PollutionData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<SpawnableBuildingData> __Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<BuildingPropertyData> __Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<ServiceUpgradeData> __Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<InfoviewMode> __Game_Prefabs_InfoviewMode_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewCoverageData> __Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewAvailabilityData> __Game_Prefabs_InfoviewAvailabilityData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewBuildingData> __Game_Prefabs_InfoviewBuildingData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewVehicleData> __Game_Prefabs_InfoviewVehicleData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewTransportStopData> __Game_Prefabs_InfoviewTransportStopData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewRouteData> __Game_Prefabs_InfoviewRouteData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewHeatmapData> __Game_Prefabs_InfoviewHeatmapData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewObjectStatusData> __Game_Prefabs_InfoviewObjectStatusData_RO_ComponentTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<InfoviewNetStatusData> __Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle;

		public BufferTypeHandle<PlaceableInfoviewItem> __Game_Prefabs_PlaceableInfoviewItem_RW_BufferTypeHandle;

		[ReadOnly]
		public ComponentTypeHandle<NetData> __Game_Prefabs_NetData_RO_ComponentTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<PlaceableInfoviewItem> __Game_Prefabs_PlaceableInfoviewItem_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubArea> __Game_Prefabs_SubArea_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<SubObject> __Game_Prefabs_SubObject_RO_BufferTypeHandle;

		[ReadOnly]
		public BufferTypeHandle<BuildingUpgradeElement> __Game_Prefabs_BuildingUpgradeElement_RO_BufferTypeHandle;

		[ReadOnly]
		public ComponentLookup<LotData> __Game_Prefabs_LotData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<ResourceData> __Game_Prefabs_ResourceData_RO_ComponentLookup;

		[ReadOnly]
		public ComponentLookup<TransportStopData> __Game_Prefabs_TransportStopData_RO_ComponentLookup;

		[ReadOnly]
		public BufferLookup<PlaceableInfoviewItem> __Game_Prefabs_PlaceableInfoviewItem_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<InfoviewMode> __Game_Prefabs_InfoviewMode_RO_BufferLookup;

		[ReadOnly]
		public BufferLookup<SubArea> __Game_Prefabs_SubArea_RO_BufferLookup;

		public BufferLookup<PlaceableInfoviewItem> __Game_Prefabs_PlaceableInfoviewItem_RW_BufferLookup;

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void __AssignHandles(ref SystemState state)
		{
			__Game_Prefabs_PrefabData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrefabData>(isReadOnly: true);
			__Unity_Entities_Entity_TypeHandle = state.GetEntityTypeHandle();
			__Game_Prefabs_InfoviewMode_RW_BufferTypeHandle = state.GetBufferTypeHandle<InfoviewMode>();
			__Game_Prefabs_InfomodeGroup_RO_ComponentLookup = state.GetComponentLookup<InfomodeGroup>(isReadOnly: true);
			__Game_Prefabs_CoverageData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<CoverageData>(isReadOnly: true);
			__Game_Prefabs_HospitalData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<HospitalData>(isReadOnly: true);
			__Game_Prefabs_PowerPlantData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PowerPlantData>(isReadOnly: true);
			__Game_Prefabs_TransformerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TransformerData>(isReadOnly: true);
			__Game_Prefabs_BatteryData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BatteryData>(isReadOnly: true);
			__Game_Prefabs_WaterPumpingStationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPumpingStationData>(isReadOnly: true);
			__Game_Prefabs_WaterTowerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterTowerData>(isReadOnly: true);
			__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SewageOutletData>(isReadOnly: true);
			__Game_Prefabs_TransportDepotData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TransportDepotData>(isReadOnly: true);
			__Game_Prefabs_TransportStationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TransportStationData>(isReadOnly: true);
			__Game_Prefabs_GarbageFacilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GarbageFacilityData>(isReadOnly: true);
			__Game_Prefabs_FireStationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<FireStationData>(isReadOnly: true);
			__Game_Prefabs_PoliceStationData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PoliceStationData>(isReadOnly: true);
			__Game_Prefabs_MaintenanceDepotData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<MaintenanceDepotData>(isReadOnly: true);
			__Game_Prefabs_PostFacilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PostFacilityData>(isReadOnly: true);
			__Game_Prefabs_TelecomFacilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TelecomFacilityData>(isReadOnly: true);
			__Game_Prefabs_SchoolData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SchoolData>(isReadOnly: true);
			__Game_Prefabs_ParkData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkData>(isReadOnly: true);
			__Game_Prefabs_EmergencyShelterData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<EmergencyShelterData>(isReadOnly: true);
			__Game_Prefabs_DisasterFacilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<DisasterFacilityData>(isReadOnly: true);
			__Game_Prefabs_FirewatchTowerData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<FirewatchTowerData>(isReadOnly: true);
			__Game_Prefabs_DeathcareFacilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<DeathcareFacilityData>(isReadOnly: true);
			__Game_Prefabs_PrisonData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PrisonData>(isReadOnly: true);
			__Game_Prefabs_AdminBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<AdminBuildingData>(isReadOnly: true);
			__Game_Prefabs_WelfareOfficeData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WelfareOfficeData>(isReadOnly: true);
			__Game_Prefabs_ResearchFacilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResearchFacilityData>(isReadOnly: true);
			__Game_Prefabs_ParkingFacilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ParkingFacilityData>(isReadOnly: true);
			__Game_Prefabs_PowerLineData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PowerLineData>(isReadOnly: true);
			__Game_Prefabs_PipelineData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PipelineData>(isReadOnly: true);
			__Game_Prefabs_ElectricityConnectionData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ElectricityConnectionData>(isReadOnly: true);
			__Game_Prefabs_WaterPipeConnectionData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPipeConnectionData>(isReadOnly: true);
			__Game_Prefabs_ResourceConnectionData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ResourceConnectionData>(isReadOnly: true);
			__Game_Prefabs_ZoneData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ZoneData>(isReadOnly: true);
			__Game_Prefabs_TransportStopData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TransportStopData>(isReadOnly: true);
			__Game_Prefabs_WorkStopData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WorkStopData>(isReadOnly: true);
			__Game_Prefabs_RouteData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<RouteData>(isReadOnly: true);
			__Game_Prefabs_TransportLineData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TransportLineData>(isReadOnly: true);
			__Game_Prefabs_ExtractorAreaData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ExtractorAreaData>(isReadOnly: true);
			__Game_Prefabs_TerraformingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<TerraformingData>(isReadOnly: true);
			__Game_Prefabs_WindPoweredData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WindPoweredData>(isReadOnly: true);
			__Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<WaterPoweredData>(isReadOnly: true);
			__Game_Prefabs_GroundWaterPoweredData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<GroundWaterPoweredData>(isReadOnly: true);
			__Game_Prefabs_PollutionData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<PollutionData>(isReadOnly: true);
			__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<SpawnableBuildingData>(isReadOnly: true);
			__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<BuildingPropertyData>(isReadOnly: true);
			__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<ServiceUpgradeData>(isReadOnly: true);
			__Game_Prefabs_InfoviewMode_RO_BufferTypeHandle = state.GetBufferTypeHandle<InfoviewMode>(isReadOnly: true);
			__Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewCoverageData>(isReadOnly: true);
			__Game_Prefabs_InfoviewAvailabilityData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewAvailabilityData>(isReadOnly: true);
			__Game_Prefabs_InfoviewBuildingData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewBuildingData>(isReadOnly: true);
			__Game_Prefabs_InfoviewVehicleData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewVehicleData>(isReadOnly: true);
			__Game_Prefabs_InfoviewTransportStopData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewTransportStopData>(isReadOnly: true);
			__Game_Prefabs_InfoviewRouteData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewRouteData>(isReadOnly: true);
			__Game_Prefabs_InfoviewHeatmapData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewHeatmapData>(isReadOnly: true);
			__Game_Prefabs_InfoviewObjectStatusData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewObjectStatusData>(isReadOnly: true);
			__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<InfoviewNetStatusData>(isReadOnly: true);
			__Game_Prefabs_PlaceableInfoviewItem_RW_BufferTypeHandle = state.GetBufferTypeHandle<PlaceableInfoviewItem>();
			__Game_Prefabs_NetData_RO_ComponentTypeHandle = state.GetComponentTypeHandle<NetData>(isReadOnly: true);
			__Game_Prefabs_PlaceableInfoviewItem_RO_BufferTypeHandle = state.GetBufferTypeHandle<PlaceableInfoviewItem>(isReadOnly: true);
			__Game_Prefabs_SubArea_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubArea>(isReadOnly: true);
			__Game_Prefabs_SubObject_RO_BufferTypeHandle = state.GetBufferTypeHandle<SubObject>(isReadOnly: true);
			__Game_Prefabs_BuildingUpgradeElement_RO_BufferTypeHandle = state.GetBufferTypeHandle<BuildingUpgradeElement>(isReadOnly: true);
			__Game_Prefabs_LotData_RO_ComponentLookup = state.GetComponentLookup<LotData>(isReadOnly: true);
			__Game_Prefabs_ResourceData_RO_ComponentLookup = state.GetComponentLookup<ResourceData>(isReadOnly: true);
			__Game_Prefabs_TransportStopData_RO_ComponentLookup = state.GetComponentLookup<TransportStopData>(isReadOnly: true);
			__Game_Prefabs_PlaceableInfoviewItem_RO_BufferLookup = state.GetBufferLookup<PlaceableInfoviewItem>(isReadOnly: true);
			__Game_Prefabs_InfoviewMode_RO_BufferLookup = state.GetBufferLookup<InfoviewMode>(isReadOnly: true);
			__Game_Prefabs_SubArea_RO_BufferLookup = state.GetBufferLookup<SubArea>(isReadOnly: true);
			__Game_Prefabs_PlaceableInfoviewItem_RW_BufferLookup = state.GetBufferLookup<PlaceableInfoviewItem>();
		}
	}

	private EntityQuery m_NewInfoviewQuery;

	private EntityQuery m_AllInfoviewQuery;

	private EntityQuery m_AllInfomodeQuery;

	private EntityQuery m_NewPlaceableQuery;

	private EntityQuery m_AllPlaceableQuery;

	private PrefabSystem m_PrefabSystem;

	private ResourceSystem m_ResourceSystem;

	private const int TYPE_PRIORITY = 1000000;

	private const int PRIMARY_REQUIREMENT_PRIORITY = 10000;

	private const int SECONDARY_REQUIREMENT_PRIORITY = 10000;

	private const int PRIMARY_EFFECT_PRIORITY = 10000;

	private const int SECONDARY_EFFECT_PRIORITY = 100;

	private TypeHandle __TypeHandle;

	public IEnumerable<InfoviewPrefab> infoviews
	{
		get
		{
			ComponentTypeHandle<PrefabData> prefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			NativeArray<ArchetypeChunk> chunks = m_AllInfoviewQuery.ToArchetypeChunkArray(Allocator.TempJob);
			try
			{
				int i = 0;
				while (i < chunks.Length)
				{
					NativeArray<PrefabData> prefabs = chunks[i].GetNativeArray(ref prefabType);
					int num;
					for (int j = 0; j < prefabs.Length; j = num)
					{
						yield return m_PrefabSystem.GetPrefab<InfoviewPrefab>(prefabs[j]);
						num = j + 1;
					}
					num = i + 1;
					i = num;
				}
			}
			finally
			{
				chunks.Dispose();
			}
		}
	}

	public IEnumerable<InfomodePrefab> infomodes
	{
		get
		{
			ComponentTypeHandle<PrefabData> prefabType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			NativeArray<ArchetypeChunk> chunks = m_AllInfomodeQuery.ToArchetypeChunkArray(Allocator.TempJob);
			try
			{
				int i = 0;
				while (i < chunks.Length)
				{
					NativeArray<PrefabData> prefabs = chunks[i].GetNativeArray(ref prefabType);
					int num;
					for (int j = 0; j < prefabs.Length; j = num)
					{
						yield return m_PrefabSystem.GetPrefab<InfomodePrefab>(prefabs[j]);
						num = j + 1;
					}
					num = i + 1;
					i = num;
				}
			}
			finally
			{
				chunks.Dispose();
			}
		}
	}

	[Preserve]
	protected override void OnCreate()
	{
		base.OnCreate();
		m_PrefabSystem = base.World.GetOrCreateSystemManaged<PrefabSystem>();
		m_ResourceSystem = base.World.GetOrCreateSystemManaged<ResourceSystem>();
		m_NewInfoviewQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<InfoviewData>(), ComponentType.ReadOnly<Created>());
		m_AllInfoviewQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<InfoviewData>());
		m_AllInfomodeQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadOnly<InfomodeData>(), ComponentType.Exclude<InfomodeGroup>());
		m_NewPlaceableQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadWrite<PlaceableInfoviewItem>(), ComponentType.ReadOnly<Created>());
		m_AllPlaceableQuery = GetEntityQuery(ComponentType.ReadOnly<PrefabData>(), ComponentType.ReadWrite<PlaceableInfoviewItem>());
	}

	[Preserve]
	protected override void OnUpdate()
	{
		if (!m_NewInfoviewQuery.IsEmptyIgnoreFilter)
		{
			base.Dependency = InitializeInfoviews(base.Dependency, m_NewInfoviewQuery, m_AllInfomodeQuery);
			base.Dependency = FindInfoviews(base.Dependency, m_AllInfoviewQuery, m_AllInfomodeQuery, m_AllPlaceableQuery);
		}
		else if (!m_NewPlaceableQuery.IsEmptyIgnoreFilter)
		{
			base.Dependency = FindInfoviews(base.Dependency, m_AllInfoviewQuery, m_AllInfomodeQuery, m_NewPlaceableQuery);
		}
	}

	private JobHandle InitializeInfoviews(JobHandle inputDeps, EntityQuery infoviewGroup, EntityQuery infomodeGroup)
	{
		NativeArray<ArchetypeChunk> nativeArray = infoviewGroup.ToArchetypeChunkArray(Allocator.TempJob);
		NativeArray<ArchetypeChunk> nativeArray2 = infomodeGroup.ToArchetypeChunkArray(Allocator.TempJob);
		NativeParallelMultiHashMap<Entity, InfoModeData> nativeParallelMultiHashMap = new NativeParallelMultiHashMap<Entity, InfoModeData>(100, Allocator.TempJob);
		try
		{
			EntityTypeHandle entityTypeHandle = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef);
			ComponentTypeHandle<PrefabData> typeHandle = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrefabData_RO_ComponentTypeHandle, ref base.CheckedStateRef);
			BufferTypeHandle<InfoviewMode> bufferTypeHandle = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewMode_RW_BufferTypeHandle, ref base.CheckedStateRef);
			ComponentLookup<InfomodeGroup> componentLookup = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_InfomodeGroup_RO_ComponentLookup, ref base.CheckedStateRef);
			inputDeps.Complete();
			for (int i = 0; i < nativeArray2.Length; i++)
			{
				ArchetypeChunk archetypeChunk = nativeArray2[i];
				NativeArray<Entity> nativeArray3 = archetypeChunk.GetNativeArray(entityTypeHandle);
				NativeArray<PrefabData> nativeArray4 = archetypeChunk.GetNativeArray(ref typeHandle);
				for (int j = 0; j < nativeArray4.Length; j++)
				{
					Entity mode = nativeArray3[j];
					PrefabData prefabData = nativeArray4[j];
					InfomodeBasePrefab prefab = m_PrefabSystem.GetPrefab<InfomodeBasePrefab>(prefabData);
					if (prefab.m_IncludeInGroups != null)
					{
						for (int k = 0; k < prefab.m_IncludeInGroups.Length; k++)
						{
							Entity entity = m_PrefabSystem.GetEntity(prefab.m_IncludeInGroups[k]);
							nativeParallelMultiHashMap.Add(entity, new InfoModeData
							{
								m_Mode = mode,
								m_Priority = prefab.m_Priority
							});
						}
					}
				}
			}
			for (int l = 0; l < nativeArray.Length; l++)
			{
				ArchetypeChunk archetypeChunk2 = nativeArray[l];
				NativeArray<PrefabData> nativeArray5 = archetypeChunk2.GetNativeArray(ref typeHandle);
				BufferAccessor<InfoviewMode> bufferAccessor = archetypeChunk2.GetBufferAccessor(ref bufferTypeHandle);
				for (int m = 0; m < bufferAccessor.Length; m++)
				{
					PrefabData prefabData2 = nativeArray5[m];
					DynamicBuffer<InfoviewMode> dynamicBuffer = bufferAccessor[m];
					InfoviewPrefab prefab2 = m_PrefabSystem.GetPrefab<InfoviewPrefab>(prefabData2);
					if (prefab2.m_Infomodes == null)
					{
						continue;
					}
					for (int n = 0; n < prefab2.m_Infomodes.Length; n++)
					{
						InfomodeInfo infomodeInfo = prefab2.m_Infomodes[n];
						Entity entity2 = m_PrefabSystem.GetEntity(infomodeInfo.m_Mode);
						if (componentLookup.HasComponent(entity2))
						{
							if (nativeParallelMultiHashMap.TryGetFirstValue(entity2, out var item, out var it))
							{
								do
								{
									int priority = infomodeInfo.m_Priority * 1000000 + infomodeInfo.m_Mode.m_Priority * 1000 + item.m_Priority;
									dynamicBuffer.Add(new InfoviewMode(item.m_Mode, priority, infomodeInfo.m_Supplemental, infomodeInfo.m_Optional));
								}
								while (nativeParallelMultiHashMap.TryGetNextValue(out item, ref it));
							}
						}
						else
						{
							int priority2 = infomodeInfo.m_Priority * 1000000 + infomodeInfo.m_Mode.m_Priority;
							dynamicBuffer.Add(new InfoviewMode(entity2, priority2, infomodeInfo.m_Supplemental, infomodeInfo.m_Optional));
						}
					}
				}
			}
		}
		finally
		{
			nativeArray.Dispose();
			nativeArray2.Dispose();
			nativeParallelMultiHashMap.Dispose();
		}
		return default(JobHandle);
	}

	private JobHandle FindInfoviews(JobHandle inputDeps, EntityQuery infoviewQuery, EntityQuery infomodeQuery, EntityQuery objectQuery)
	{
		NativeQueue<InfoviewBufferData> infoViewBuffer = new NativeQueue<InfoviewBufferData>(Allocator.TempJob);
		JobHandle outJobHandle;
		NativeList<ArchetypeChunk> infoviewChunks = infoviewQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle);
		JobHandle outJobHandle2;
		NativeList<ArchetypeChunk> infomodeChunks = infomodeQuery.ToArchetypeChunkListAsync(Allocator.TempJob, out outJobHandle2);
		FindInfoviewJob jobData = new FindInfoviewJob
		{
			m_InfoviewChunks = infoviewChunks,
			m_InfomodeChunks = infomodeChunks,
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_CoverageType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_CoverageData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_HospitalType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_HospitalData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PowerPlantType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PowerPlantData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransformerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TransformerData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BatteryType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BatteryData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterPumpingStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WaterPumpingStationData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterTowerType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WaterTowerData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SewageOutletType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SewageOutletData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransportDepotType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TransportDepotData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransportStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TransportStationData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GarbageFacilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_GarbageFacilityData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_FireStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_FireStationData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PoliceStationType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PoliceStationData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_MaintenanceDepotType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_MaintenanceDepotData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PostFacilityDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PostFacilityData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TelecomFacilityDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TelecomFacilityData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SchoolDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SchoolData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ParkData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_EmergencyShelterDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_EmergencyShelterData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DisasterFacilityDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_DisasterFacilityData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_FirewatchTowerDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_FirewatchTowerData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_DeathcareFacilityDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_DeathcareFacilityData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PrisonDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PrisonData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_AdminBuildingDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_AdminBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WelfareOfficeDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WelfareOfficeData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResearchFacilityDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ResearchFacilityData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ParkingFacilityDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ParkingFacilityData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PowerLineType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PowerLineData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PipelineType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PipelineData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ElectricityConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ElectricityConnectionData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterPipeConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WaterPipeConnectionData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ResourceConnectionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ResourceConnectionData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ZoneType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ZoneData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransportStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TransportStopData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WorkStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WorkStopData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_RouteType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_RouteData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TransportLineType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TransportLineData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ExtractorAreaType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ExtractorAreaData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_TerraformingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_TerraformingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WindPoweredType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WindPoweredData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_WaterPoweredType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_WaterPoweredData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_GroundWaterPoweredType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_GroundWaterPoweredData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PollutionType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_PollutionData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_SpawnableBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingPropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_ServiceUpgradeType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_ServiceUpgradeData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewModeType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewMode_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_InfoviewCoverageType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewCoverageData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewAvailabilityType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewAvailabilityData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewVehicleType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewVehicleData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewTransportStopType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewTransportStopData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewRouteType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewRouteData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewHeatmapType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewHeatmapData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewObjectStatusType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewObjectStatusData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_InfoviewNetStatusType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_InfoviewNetStatusData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PlaceableInfoviewType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_PlaceableInfoviewItem_RW_BufferTypeHandle, ref base.CheckedStateRef)
		};
		FindSubInfoviewJob jobData2 = new FindSubInfoviewJob
		{
			m_EntityType = InternalCompilerInterface.GetEntityTypeHandle(ref __TypeHandle.__Unity_Entities_Entity_TypeHandle, ref base.CheckedStateRef),
			m_SpawnableBuildingType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_SpawnableBuildingData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_BuildingPropertyType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingPropertyData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_NetDataType = InternalCompilerInterface.GetComponentTypeHandle(ref __TypeHandle.__Game_Prefabs_NetData_RO_ComponentTypeHandle, ref base.CheckedStateRef),
			m_PlaceableInfoviewType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_PlaceableInfoviewItem_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubAreaType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubArea_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_SubObjectType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_SubObject_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_BuildingUpgradeElementType = InternalCompilerInterface.GetBufferTypeHandle(ref __TypeHandle.__Game_Prefabs_BuildingUpgradeElement_RO_BufferTypeHandle, ref base.CheckedStateRef),
			m_LotData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_LotData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_ResourceData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_ResourceData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_TransportStopData = InternalCompilerInterface.GetComponentLookup(ref __TypeHandle.__Game_Prefabs_TransportStopData_RO_ComponentLookup, ref base.CheckedStateRef),
			m_PlaceableInfoviewData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceableInfoviewItem_RO_BufferLookup, ref base.CheckedStateRef),
			m_InfoviewModes = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_InfoviewMode_RO_BufferLookup, ref base.CheckedStateRef),
			m_SubAreas = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_SubArea_RO_BufferLookup, ref base.CheckedStateRef),
			m_ResourcePrefabs = m_ResourceSystem.GetPrefabs(),
			m_InfoViewBuffer = infoViewBuffer.AsParallelWriter()
		};
		AssignInfoviewJob jobData3 = new AssignInfoviewJob
		{
			m_InfoViewBuffer = infoViewBuffer,
			m_PlaceableInfoviewData = InternalCompilerInterface.GetBufferLookup(ref __TypeHandle.__Game_Prefabs_PlaceableInfoviewItem_RW_BufferLookup, ref base.CheckedStateRef)
		};
		JobHandle jobHandle = JobChunkExtensions.ScheduleParallel(jobData, objectQuery, JobHandle.CombineDependencies(inputDeps, outJobHandle, outJobHandle2));
		JobHandle dependsOn = JobChunkExtensions.ScheduleParallel(jobData2, objectQuery, jobHandle);
		JobHandle jobHandle2 = IJobExtensions.Schedule(jobData3, dependsOn);
		infoViewBuffer.Dispose(jobHandle2);
		infoviewChunks.Dispose(jobHandle);
		infomodeChunks.Dispose(jobHandle);
		return jobHandle2;
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
	public InfoviewInitializeSystem()
	{
	}
}
