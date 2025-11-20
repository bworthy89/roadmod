using System;
using Colossal.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class ElectricityParametersMode : EntityQueryModePrefab
{
	[Range(0f, 1f)]
	public float m_InitialBatteryCharge = 0.1f;

	public AnimationCurve m_TemperatureConsumptionMultiplier;

	[Range(0f, 1f)]
	public float m_CloudinessSolarPenalty = 0.25f;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<ElectricityParameterData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<ElectricityParameterData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		ElectricityParameterData componentData = entityManager.GetComponentData<ElectricityParameterData>(singletonEntity);
		componentData.m_InitialBatteryCharge = m_InitialBatteryCharge;
		componentData.m_TemperatureConsumptionMultiplier = new AnimationCurve1(m_TemperatureConsumptionMultiplier);
		componentData.m_CloudinessSolarPenalty = m_CloudinessSolarPenalty;
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		ElectricityParametersPrefab electricityParametersPrefab = prefabSystem.GetPrefab<ElectricityParametersPrefab>(entity);
		ElectricityParameterData componentData = entityManager.GetComponentData<ElectricityParameterData>(entity);
		componentData.m_InitialBatteryCharge = electricityParametersPrefab.m_InitialBatteryCharge;
		componentData.m_TemperatureConsumptionMultiplier = new AnimationCurve1(electricityParametersPrefab.m_TemperatureConsumptionMultiplier);
		componentData.m_CloudinessSolarPenalty = electricityParametersPrefab.m_CloudinessSolarPenalty;
		entityManager.SetComponentData(entity, componentData);
	}
}
