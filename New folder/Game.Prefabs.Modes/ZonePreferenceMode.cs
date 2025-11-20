using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class ZonePreferenceMode : EntityQueryModePrefab
{
	public float m_ResidentialSignificanceServices;

	public float m_ResidentialSignificanceWorkplaces;

	public float m_ResidentialSignificanceLandValue;

	public float m_ResidentialSignificancePollution;

	public float m_ResidentialNeutralLandValue;

	public float m_CommercialSignificanceConsumers;

	public float m_CommercialSignificanceCompetitors;

	public float m_CommercialSignificanceWorkplaces;

	public float m_CommercialSignificanceLandValue;

	public float m_CommercialNeutralLandValue;

	public float m_IndustrialSignificanceInput;

	public float m_IndustrialSignificanceOutside;

	public float m_IndustrialSignificanceLandValue;

	public float m_IndustrialNeutralLandValue;

	public float m_OfficeSignificanceEmployees;

	public float m_OfficeSignificanceServices;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<ZonePreferenceData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<ZonePreferenceData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		ZonePreferenceData componentData = entityManager.GetComponentData<ZonePreferenceData>(singletonEntity);
		componentData.m_ResidentialSignificanceServices = m_ResidentialSignificanceServices;
		componentData.m_ResidentialSignificanceWorkplaces = m_ResidentialSignificanceWorkplaces;
		componentData.m_ResidentialSignificanceLandValue = m_ResidentialSignificanceLandValue;
		componentData.m_ResidentialSignificancePollution = m_ResidentialSignificancePollution;
		componentData.m_ResidentialNeutralLandValue = m_ResidentialNeutralLandValue;
		componentData.m_CommercialSignificanceConsumers = m_CommercialSignificanceConsumers;
		componentData.m_CommercialSignificanceCompetitors = m_CommercialSignificanceCompetitors;
		componentData.m_CommercialSignificanceWorkplaces = m_CommercialSignificanceWorkplaces;
		componentData.m_CommercialSignificanceLandValue = m_CommercialSignificanceLandValue;
		componentData.m_CommercialNeutralLandValue = m_CommercialNeutralLandValue;
		componentData.m_IndustrialSignificanceInput = m_IndustrialSignificanceInput;
		componentData.m_IndustrialSignificanceOutside = m_IndustrialSignificanceOutside;
		componentData.m_IndustrialSignificanceLandValue = m_IndustrialSignificanceLandValue;
		componentData.m_IndustrialNeutralLandValue = m_IndustrialNeutralLandValue;
		componentData.m_OfficeSignificanceEmployees = m_OfficeSignificanceEmployees;
		componentData.m_OfficeSignificanceServices = m_OfficeSignificanceServices;
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		ZonePreferencePrefab zonePreferencePrefab = prefabSystem.GetPrefab<ZonePreferencePrefab>(entity);
		ZonePreferenceData componentData = entityManager.GetComponentData<ZonePreferenceData>(entity);
		componentData.m_ResidentialSignificanceServices = zonePreferencePrefab.m_ResidentialSignificanceServices;
		componentData.m_ResidentialSignificanceWorkplaces = zonePreferencePrefab.m_ResidentialSignificanceWorkplaces;
		componentData.m_ResidentialSignificanceLandValue = zonePreferencePrefab.m_ResidentialSignificanceLandValue;
		componentData.m_ResidentialSignificancePollution = zonePreferencePrefab.m_ResidentialSignificancePollution;
		componentData.m_ResidentialNeutralLandValue = zonePreferencePrefab.m_ResidentialNeutralLandValue;
		componentData.m_CommercialSignificanceConsumers = zonePreferencePrefab.m_CommercialSignificanceConsumers;
		componentData.m_CommercialSignificanceCompetitors = zonePreferencePrefab.m_CommercialSignificanceCompetitors;
		componentData.m_CommercialSignificanceWorkplaces = zonePreferencePrefab.m_CommercialSignificanceWorkplaces;
		componentData.m_CommercialSignificanceLandValue = zonePreferencePrefab.m_CommercialSignificanceLandValue;
		componentData.m_CommercialNeutralLandValue = zonePreferencePrefab.m_CommercialNeutralLandValue;
		componentData.m_IndustrialSignificanceInput = zonePreferencePrefab.m_IndustrialSignificanceInput;
		componentData.m_IndustrialSignificanceOutside = zonePreferencePrefab.m_IndustrialSignificanceOutside;
		componentData.m_IndustrialSignificanceLandValue = zonePreferencePrefab.m_IndustrialSignificanceLandValue;
		componentData.m_IndustrialNeutralLandValue = zonePreferencePrefab.m_IndustrialNeutralLandValue;
		componentData.m_OfficeSignificanceEmployees = zonePreferencePrefab.m_OfficeSignificanceEmployees;
		componentData.m_OfficeSignificanceServices = zonePreferencePrefab.m_OfficeSignificanceServices;
		entityManager.SetComponentData(entity, componentData);
	}
}
