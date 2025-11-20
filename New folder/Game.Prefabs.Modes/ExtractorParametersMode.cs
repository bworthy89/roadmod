using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class ExtractorParametersMode : EntityQueryModePrefab
{
	public float m_FertilityConsumption;

	public float m_OreConsumption;

	public float m_ForestConsumption;

	public float m_OilConsumption;

	public float m_FullFertility;

	public float m_FullOre;

	public float m_FullOil;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<ExtractorParameterData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<ExtractorParameterData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		ExtractorParameterData componentData = entityManager.GetComponentData<ExtractorParameterData>(singletonEntity);
		componentData.m_FertilityConsumption = m_FertilityConsumption;
		componentData.m_OreConsumption = m_OreConsumption;
		componentData.m_ForestConsumption = m_ForestConsumption;
		componentData.m_OilConsumption = m_OilConsumption;
		componentData.m_FullFertility = m_FullFertility;
		componentData.m_FullOre = m_FullOre;
		componentData.m_FullOil = m_FullOil;
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		ExtractorParameterPrefab extractorParameterPrefab = prefabSystem.GetPrefab<ExtractorParameterPrefab>(entity);
		ExtractorParameterData componentData = entityManager.GetComponentData<ExtractorParameterData>(entity);
		componentData.m_FertilityConsumption = extractorParameterPrefab.m_FertilityConsumption;
		componentData.m_OreConsumption = extractorParameterPrefab.m_OreConsumption;
		componentData.m_ForestConsumption = extractorParameterPrefab.m_ForestConsumption;
		componentData.m_OilConsumption = extractorParameterPrefab.m_OilConsumption;
		componentData.m_FullFertility = extractorParameterPrefab.m_FullFertility;
		componentData.m_FullOre = extractorParameterPrefab.m_FullOre;
		componentData.m_FullOil = extractorParameterPrefab.m_FullOil;
		entityManager.SetComponentData(entity, componentData);
	}
}
