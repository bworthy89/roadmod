using System;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class AttractivenessParametersMode : EntityQueryModePrefab
{
	public float m_ForestEffect;

	public float m_ForestDistance;

	public float m_ShoreEffect;

	public float m_ShoreDistance;

	public float3 m_HeightBonus;

	public float2 m_AttractiveTemperature;

	public float2 m_ExtremeTemperature;

	public float2 m_TemperatureAffect;

	public float2 m_RainEffectRange;

	public float2 m_SnowEffectRange;

	public float3 m_SnowRainExtremeAffect;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<AttractivenessParameterData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<AttractivenessParameterData>(entity);
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		AttractivenessParametersPrefab attractivenessParametersPrefab = prefabSystem.GetPrefab<AttractivenessParametersPrefab>(entity);
		AttractivenessParameterData componentData = entityManager.GetComponentData<AttractivenessParameterData>(entity);
		componentData.m_ForestEffect = attractivenessParametersPrefab.m_ForestEffect;
		componentData.m_ForestDistance = attractivenessParametersPrefab.m_ForestDistance;
		componentData.m_ShoreEffect = attractivenessParametersPrefab.m_ShoreEffect;
		componentData.m_ShoreDistance = attractivenessParametersPrefab.m_ShoreDistance;
		componentData.m_HeightBonus = attractivenessParametersPrefab.m_HeightBonus;
		componentData.m_AttractiveTemperature = attractivenessParametersPrefab.m_AttractiveTemperature;
		componentData.m_ExtremeTemperature = attractivenessParametersPrefab.m_ExtremeTemperature;
		componentData.m_TemperatureAffect = attractivenessParametersPrefab.m_TemperatureAffect;
		componentData.m_RainEffectRange = attractivenessParametersPrefab.m_RainEffectRange;
		componentData.m_SnowEffectRange = attractivenessParametersPrefab.m_SnowEffectRange;
		componentData.m_SnowRainExtremeAffect = attractivenessParametersPrefab.m_SnowRainExtremeAffect;
		entityManager.SetComponentData(entity, componentData);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		AttractivenessParameterData componentData = entityManager.GetComponentData<AttractivenessParameterData>(singletonEntity);
		componentData.m_ForestEffect = m_ForestEffect;
		componentData.m_ForestDistance = m_ForestDistance;
		componentData.m_ShoreEffect = m_ShoreEffect;
		componentData.m_ShoreDistance = m_ShoreDistance;
		componentData.m_HeightBonus = m_HeightBonus;
		componentData.m_AttractiveTemperature = m_AttractiveTemperature;
		componentData.m_ExtremeTemperature = m_ExtremeTemperature;
		componentData.m_TemperatureAffect = m_TemperatureAffect;
		componentData.m_RainEffectRange = m_RainEffectRange;
		componentData.m_SnowEffectRange = m_SnowEffectRange;
		componentData.m_SnowRainExtremeAffect = m_SnowRainExtremeAffect;
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}
}
