using System;
using Colossal.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class ServiceFeeParameterMode : EntityQueryModePrefab
{
	public FeeParameters m_ElectricityFee;

	public AnimationCurve m_ElectricityFeeConsumptionMultiplier;

	public FeeParameters m_HealthcareFee;

	public FeeParameters m_BasicEducationFee;

	public FeeParameters m_SecondaryEducationFee;

	public FeeParameters m_HigherEducationFee;

	public FeeParameters m_WaterFee;

	public AnimationCurve m_WaterFeeConsumptionMultiplier;

	public FeeParameters m_GarbageFee;

	public int4 m_GarbageFeeRCIO;

	public FeeParameters m_FireResponseFee;

	public FeeParameters m_PoliceFee;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<ServiceFeeParameterData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<ServiceFeeParameterData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		ServiceFeeParameterData componentData = entityManager.GetComponentData<ServiceFeeParameterData>(singletonEntity);
		componentData.m_ElectricityFee = m_ElectricityFee;
		componentData.m_ElectricityFeeConsumptionMultiplier = new AnimationCurve1(m_ElectricityFeeConsumptionMultiplier);
		componentData.m_HealthcareFee = m_HealthcareFee;
		componentData.m_BasicEducationFee = m_BasicEducationFee;
		componentData.m_SecondaryEducationFee = m_SecondaryEducationFee;
		componentData.m_HigherEducationFee = m_HigherEducationFee;
		componentData.m_WaterFee = m_WaterFee;
		componentData.m_WaterFeeConsumptionMultiplier = new AnimationCurve1(m_WaterFeeConsumptionMultiplier);
		componentData.m_GarbageFee = m_GarbageFee;
		componentData.m_GarbageFeeRCIO = m_GarbageFeeRCIO;
		componentData.m_FireResponseFee = m_FireResponseFee;
		componentData.m_PoliceFee = m_PoliceFee;
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		ServiceFeeParameterPrefab serviceFeeParameterPrefab = prefabSystem.GetPrefab<ServiceFeeParameterPrefab>(entity);
		ServiceFeeParameterData componentData = entityManager.GetComponentData<ServiceFeeParameterData>(entity);
		componentData.m_ElectricityFee = serviceFeeParameterPrefab.m_ElectricityFee;
		componentData.m_ElectricityFeeConsumptionMultiplier = new AnimationCurve1(serviceFeeParameterPrefab.m_ElectricityFeeConsumptionMultiplier);
		componentData.m_HealthcareFee = serviceFeeParameterPrefab.m_HealthcareFee;
		componentData.m_BasicEducationFee = serviceFeeParameterPrefab.m_BasicEducationFee;
		componentData.m_SecondaryEducationFee = serviceFeeParameterPrefab.m_SecondaryEducationFee;
		componentData.m_HigherEducationFee = serviceFeeParameterPrefab.m_HigherEducationFee;
		componentData.m_WaterFee = serviceFeeParameterPrefab.m_WaterFee;
		componentData.m_WaterFeeConsumptionMultiplier = new AnimationCurve1(serviceFeeParameterPrefab.m_WaterFeeConsumptionMultiplier);
		componentData.m_GarbageFee = serviceFeeParameterPrefab.m_GarbageFee;
		componentData.m_GarbageFeeRCIO = serviceFeeParameterPrefab.m_GarbageFeeRCIO;
		componentData.m_FireResponseFee = serviceFeeParameterPrefab.m_FireResponseFee;
		componentData.m_PoliceFee = serviceFeeParameterPrefab.m_PoliceFee;
		entityManager.SetComponentData(entity, componentData);
	}
}
