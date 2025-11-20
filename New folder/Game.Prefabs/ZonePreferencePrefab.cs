using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Zones/", new Type[] { })]
public class ZonePreferencePrefab : PrefabBase
{
	public float m_ResidentialSignificanceServices = 100f;

	public float m_ResidentialSignificanceWorkplaces = 50f;

	public float m_ResidentialSignificanceLandValue = -1f;

	[Tooltip("The pollution factor that affect the residential suitability, x is the ground pollution, y is noise pollution, z is air pollution")]
	public float3 m_ResidentialSignificancePollution = new float3(-100f, -100f, -100f);

	public float m_ResidentialNeutralLandValue = 10f;

	public float m_CommercialSignificanceConsumers = 100f;

	public float m_CommercialSignificanceCompetitors = 200f;

	public float m_CommercialSignificanceWorkplaces = 70f;

	public float m_CommercialSignificanceLandValue = -0.5f;

	public float m_CommercialNeutralLandValue = 20f;

	public float m_IndustrialSignificanceInput = 100f;

	public float m_IndustrialSignificanceOutside = 100f;

	public float m_IndustrialSignificanceLandValue = -1f;

	public float m_IndustrialNeutralLandValue = 5f;

	public float m_OfficeSignificanceEmployees = 100f;

	public float m_OfficeSignificanceServices = 100f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ZonePreferenceData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.SetComponentData(entity, new ZonePreferenceData
		{
			m_ResidentialSignificanceServices = m_ResidentialSignificanceServices,
			m_ResidentialSignificanceWorkplaces = m_ResidentialSignificanceWorkplaces,
			m_ResidentialSignificanceLandValue = m_ResidentialSignificanceLandValue,
			m_ResidentialSignificancePollution = m_ResidentialSignificancePollution,
			m_ResidentialNeutralLandValue = m_ResidentialNeutralLandValue,
			m_CommercialSignificanceCompetitors = m_CommercialSignificanceCompetitors,
			m_CommercialSignificanceConsumers = m_CommercialSignificanceConsumers,
			m_CommercialSignificanceWorkplaces = m_CommercialSignificanceWorkplaces,
			m_CommercialSignificanceLandValue = m_CommercialSignificanceLandValue,
			m_CommercialNeutralLandValue = m_CommercialNeutralLandValue,
			m_IndustrialSignificanceInput = m_IndustrialSignificanceInput,
			m_IndustrialSignificanceLandValue = m_IndustrialSignificanceLandValue,
			m_IndustrialSignificanceOutside = m_IndustrialSignificanceOutside,
			m_IndustrialNeutralLandValue = m_IndustrialNeutralLandValue,
			m_OfficeSignificanceEmployees = m_OfficeSignificanceEmployees,
			m_OfficeSignificanceServices = m_OfficeSignificanceServices
		});
	}
}
