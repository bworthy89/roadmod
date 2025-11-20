using System;
using System.Collections.Generic;
using Game.Net;
using Game.Pathfind;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[] { typeof(StaticObjectPrefab) })]
public class ServiceCoverage : ComponentBase
{
	public float m_Range = 1000f;

	public float m_Capacity = 3000f;

	public float m_Magnitude = 1f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<CoverageData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<CoverageServiceType>());
		components.Add(ComponentType.ReadWrite<CoverageElement>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		CoverageData componentData = new CoverageData
		{
			m_Range = m_Range,
			m_Capacity = m_Capacity,
			m_Magnitude = m_Magnitude
		};
		if (entityManager.HasComponent<HospitalData>(entity))
		{
			componentData.m_Service = CoverageService.Healthcare;
		}
		else if (entityManager.HasComponent<FireStationData>(entity))
		{
			componentData.m_Service = CoverageService.FireRescue;
		}
		else if (entityManager.HasComponent<PoliceStationData>(entity))
		{
			componentData.m_Service = CoverageService.Police;
		}
		else if (entityManager.HasComponent<ParkData>(entity))
		{
			componentData.m_Service = CoverageService.Park;
		}
		else if (entityManager.HasComponent<PostFacilityData>(entity) || entityManager.HasComponent<MailBoxData>(entity))
		{
			componentData.m_Service = CoverageService.PostService;
		}
		else if (entityManager.HasComponent<SchoolData>(entity))
		{
			componentData.m_Service = CoverageService.Education;
		}
		else if (entityManager.HasComponent<EmergencyShelterData>(entity))
		{
			componentData.m_Service = CoverageService.EmergencyShelter;
		}
		else if (entityManager.HasComponent<WelfareOfficeData>(entity))
		{
			componentData.m_Service = CoverageService.Welfare;
		}
		else
		{
			ComponentBase.baseLog.ErrorFormat(base.prefab, "Unknown coverage service type: {0}", base.prefab.name);
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
