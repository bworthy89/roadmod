using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class HappinessFactorParameterMode : EntityQueryModePrefab
{
	public int m_TaxBaseLevel;

	public int taxIndex => 17;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<HappinessFactorParameterData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetBuffer<HappinessFactorParameterData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		DynamicBuffer<HappinessFactorParameterData> buffer = entityManager.GetBuffer<HappinessFactorParameterData>(singletonEntity);
		HappinessFactorParameterData value = buffer[taxIndex];
		value.m_BaseLevel = m_TaxBaseLevel;
		buffer[taxIndex] = value;
		return deps;
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		HappinessFactorParameterPrefab happinessFactorParameterPrefab = prefabSystem.GetPrefab<HappinessFactorParameterPrefab>(entity);
		DynamicBuffer<HappinessFactorParameterData> buffer = entityManager.GetBuffer<HappinessFactorParameterData>(entity);
		HappinessFactorParameterData value = buffer[taxIndex];
		value.m_BaseLevel = happinessFactorParameterPrefab.m_BaseLevels[taxIndex];
		buffer[taxIndex] = value;
	}
}
