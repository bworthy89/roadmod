using Game.Economy;
using Game.Net;
using Game.Prefabs;
using Game.Simulation;
using Game.Zones;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

public static class InfoviewUtils
{
	public static float GetColor(InfoviewCoverageData data, float coverage)
	{
		return math.saturate((coverage - data.m_Range.min) / (data.m_Range.max - data.m_Range.min));
	}

	public static float GetColor(InfoviewAvailabilityData data, DynamicBuffer<ResourceAvailability> availabilityBuffer, float curvePosition, ref ZonePreferenceData preferences, NativeArray<int> industrialDemands, NativeArray<int> storageDemands, float3 pollution, float landValue, DynamicBuffer<ProcessEstimate> estimates, NativeList<IndustrialProcessData> processes, ResourcePrefabs resourcePrefabs, ComponentLookup<ResourceData> resourceDatas)
	{
		Resource allResources = EconomyUtils.GetAllResources();
		BuildingPropertyData propertyData = new BuildingPropertyData
		{
			m_AllowedManufactured = allResources,
			m_AllowedInput = allResources,
			m_AllowedSold = allResources,
			m_AllowedStored = allResources,
			m_ResidentialProperties = 1
		};
		float num = ZoneEvaluationUtils.GetScore(data.m_AreaType, data.m_Office, availabilityBuffer, curvePosition, ref preferences, storage: false, industrialDemands, propertyData, pollution, landValue, estimates, processes, resourcePrefabs, ref resourceDatas) / 256f;
		if (data.m_AreaType == AreaType.Industrial && !data.m_Office)
		{
			num *= 0.875f;
			num += ZoneEvaluationUtils.GetScore(data.m_AreaType, office: false, availabilityBuffer, curvePosition, ref preferences, storage: true, storageDemands, propertyData, pollution, landValue, estimates, processes, resourcePrefabs, ref resourceDatas) / 2048f;
		}
		return math.saturate(num);
	}

	public static float GetColor(InfoviewNetStatusData data, float status)
	{
		return math.saturate((status - data.m_Range.min) / (data.m_Range.max - data.m_Range.min));
	}

	public static float GetColor(InfoviewBuildingStatusData data, float status)
	{
		return math.saturate((status - data.m_Range.min) / (data.m_Range.max - data.m_Range.min));
	}

	public static float GetColor(InfoviewObjectStatusData data, float status)
	{
		return math.saturate((status - data.m_Range.min) / (data.m_Range.max - data.m_Range.min));
	}
}
