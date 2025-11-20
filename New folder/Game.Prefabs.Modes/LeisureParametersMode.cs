using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class LeisureParametersMode : EntityQueryModePrefab
{
	public int m_LeisureRandomFactor;

	public int m_TouristLodgingConsumePerDay;

	public int m_TouristServiceConsumePerDay;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<LeisureParametersData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<LeisureParametersData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		LeisureParametersData componentData = entityManager.GetComponentData<LeisureParametersData>(singletonEntity);
		componentData.m_LeisureRandomFactor = m_LeisureRandomFactor;
		componentData.m_TouristLodgingConsumePerDay = m_TouristLodgingConsumePerDay;
		componentData.m_TouristServiceConsumePerDay = m_TouristServiceConsumePerDay;
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		LeisureParametersPrefab leisureParametersPrefab = prefabSystem.GetPrefab<LeisureParametersPrefab>(entity);
		LeisureParametersData componentData = entityManager.GetComponentData<LeisureParametersData>(entity);
		componentData.m_LeisureRandomFactor = leisureParametersPrefab.m_LeisureRandomFactor;
		componentData.m_TouristLodgingConsumePerDay = leisureParametersPrefab.m_TouristLodgingConsumePerDay;
		componentData.m_TouristServiceConsumePerDay = leisureParametersPrefab.m_TouristServiceConsumePerDay;
		entityManager.SetComponentData(entity, componentData);
	}
}
