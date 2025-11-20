using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class SoilWaterMode : EntityQueryModePrefab
{
	public float m_RainMultiplier;

	public float m_HeightEffect;

	public float m_MaxDiffusion;

	public float m_WaterPerUnit;

	public float m_MoistureUnderWater;

	public float m_MaximumWaterDepth;

	public float m_OverflowRate;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<SoilWaterParameterData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<SoilWaterParameterData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		SoilWaterParameterData componentData = entityManager.GetComponentData<SoilWaterParameterData>(singletonEntity);
		componentData.m_RainMultiplier = m_RainMultiplier;
		componentData.m_HeightEffect = m_HeightEffect;
		componentData.m_MaxDiffusion = m_MaxDiffusion;
		componentData.m_WaterPerUnit = m_WaterPerUnit;
		componentData.m_MoistureUnderWater = m_MoistureUnderWater;
		componentData.m_MaximumWaterDepth = m_MaximumWaterDepth;
		componentData.m_OverflowRate = m_OverflowRate;
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		SoilWaterPrefab soilWaterPrefab = prefabSystem.GetPrefab<SoilWaterPrefab>(entity);
		SoilWaterParameterData componentData = entityManager.GetComponentData<SoilWaterParameterData>(entity);
		componentData.m_RainMultiplier = soilWaterPrefab.m_RainMultiplier;
		componentData.m_HeightEffect = soilWaterPrefab.m_HeightEffect;
		componentData.m_MaxDiffusion = soilWaterPrefab.m_MaxDiffusion;
		componentData.m_WaterPerUnit = soilWaterPrefab.m_WaterPerUnit;
		componentData.m_MoistureUnderWater = soilWaterPrefab.m_MoistureUnderWater;
		componentData.m_MaximumWaterDepth = soilWaterPrefab.m_MaximumWaterDepth;
		componentData.m_OverflowRate = soilWaterPrefab.m_OverflowRate;
		entityManager.SetComponentData(entity, componentData);
	}
}
