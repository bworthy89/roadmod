using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class XPParametersMode : EntityQueryModePrefab
{
	public float m_XPPerPopulation;

	public float m_XPPerHappiness;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<XPParameterData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<XPParameterData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		XPParameterData componentData = entityManager.GetComponentData<XPParameterData>(singletonEntity);
		componentData.m_XPPerPopulation = m_XPPerPopulation;
		componentData.m_XPPerHappiness = m_XPPerHappiness;
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		XPParametersPrefab xPParametersPrefab = prefabSystem.GetPrefab<XPParametersPrefab>(entity);
		XPParameterData componentData = entityManager.GetComponentData<XPParameterData>(entity);
		componentData.m_XPPerPopulation = xPParametersPrefab.m_XPPerPopulation;
		componentData.m_XPPerHappiness = xPParametersPrefab.m_XPPerHappiness;
		entityManager.SetComponentData(entity, componentData);
	}
}
