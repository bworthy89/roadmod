using System;
using Colossal.Collections;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Parameters/", new Type[] { })]
public class EconomyParametersMode : EntityQueryModePrefab
{
	public float m_ExtractorCompanyExportMultiplier;

	public int m_Wage0;

	public int m_Wage1;

	public int m_Wage2;

	public int m_Wage3;

	public int m_Wage4;

	public float m_CommuterWageMultiplier;

	public float m_CityServiceWageAdjustment;

	public int m_CompanyBankruptcyLimit;

	public int m_ResidentialMinimumEarnings;

	public int m_UnemploymentBenefit;

	public int m_Pension;

	public int m_FamilyAllowance;

	public float2 m_ResourceConsumptionMultiplier;

	public float m_ResourceConsumptionPerCitizen;

	public float m_TouristConsumptionMultiplier;

	public float m_WorkDayStart;

	public float m_WorkDayEnd;

	public float m_IndustrialEfficiency;

	public float m_CommercialEfficiency;

	public float m_ExtractorProductionEfficiency;

	public float m_TrafficReduction;

	public float m_MaxCitySpecializationBonus;

	public int m_ResourceProductionCoefficient;

	public float m_MixedBuildingCompanyRentPercentage;

	public float3 m_LandValueModifier;

	public float3 m_RentPriceBuildingZoneTypeBase;

	public float m_ResidentialUpkeepLevelExponent;

	public float m_CommercialUpkeepLevelExponent;

	public float m_IndustrialUpkeepLevelExponent;

	public int m_PerOfficeResourceNeededForIndustrial;

	public float m_UnemploymentAllowanceMaxDays;

	public int m_ShopPossibilityIncreaseDivider;

	public int m_PlayerStartMoney;

	public float3 m_BuildRefundPercentage;

	public float3 m_BuildRefundTimeRange;

	public float m_RelocationCostMultiplierOverride;

	public float3 m_RoadRefundPercentage;

	public float3 m_RoadRefundTimeRange;

	public int3 m_TreeCostMultipliers;

	public AnimationCurve m_MapTileUpkeepCostMultiplier;

	public float2 m_LoanMinMaxInterestRate;

	public override EntityQueryDesc GetEntityQueryDesc()
	{
		EntityQueryDesc entityQueryDesc = new EntityQueryDesc();
		entityQueryDesc.All = new ComponentType[1] { ComponentType.ReadOnly<EconomyParameterData>() };
		return entityQueryDesc;
	}

	protected override void RecordChanges(EntityManager entityManager, Entity entity)
	{
		entityManager.GetComponentData<EconomyParameterData>(entity);
	}

	public override JobHandle ApplyModeData(EntityManager entityManager, EntityQuery requestedQuery, JobHandle deps)
	{
		Entity singletonEntity = requestedQuery.GetSingletonEntity();
		EconomyParameterData componentData = entityManager.GetComponentData<EconomyParameterData>(singletonEntity);
		componentData.m_ExtractorCompanyExportMultiplier = m_ExtractorCompanyExportMultiplier;
		componentData.m_Wage0 = m_Wage0;
		componentData.m_Wage1 = m_Wage1;
		componentData.m_Wage2 = m_Wage2;
		componentData.m_Wage3 = m_Wage3;
		componentData.m_Wage4 = m_Wage4;
		componentData.m_CommuterWageMultiplier = m_CommuterWageMultiplier;
		componentData.m_CityServiceWageAdjustment = m_CityServiceWageAdjustment;
		componentData.m_CompanyBankruptcyLimit = m_CompanyBankruptcyLimit;
		componentData.m_ResidentialMinimumEarnings = m_ResidentialMinimumEarnings;
		componentData.m_UnemploymentBenefit = m_UnemploymentBenefit;
		componentData.m_Pension = m_Pension;
		componentData.m_FamilyAllowance = m_FamilyAllowance;
		componentData.m_ResourceConsumptionMultiplier = m_ResourceConsumptionMultiplier;
		componentData.m_ResourceConsumptionPerCitizen = m_ResourceConsumptionPerCitizen;
		componentData.m_TouristConsumptionMultiplier = m_TouristConsumptionMultiplier;
		componentData.m_WorkDayStart = m_WorkDayStart;
		componentData.m_WorkDayEnd = m_WorkDayEnd;
		componentData.m_IndustrialEfficiency = m_IndustrialEfficiency;
		componentData.m_CommercialEfficiency = m_CommercialEfficiency;
		componentData.m_ExtractorProductionEfficiency = m_ExtractorProductionEfficiency;
		componentData.m_TrafficReduction = m_TrafficReduction;
		componentData.m_MaxCitySpecializationBonus = m_MaxCitySpecializationBonus;
		componentData.m_ResourceProductionCoefficient = m_ResourceProductionCoefficient;
		componentData.m_MixedBuildingCompanyRentPercentage = m_MixedBuildingCompanyRentPercentage;
		componentData.m_LandValueModifier = m_LandValueModifier;
		componentData.m_RentPriceBuildingZoneTypeBase = m_RentPriceBuildingZoneTypeBase;
		componentData.m_ResidentialUpkeepLevelExponent = m_ResidentialUpkeepLevelExponent;
		componentData.m_CommercialUpkeepLevelExponent = m_CommercialUpkeepLevelExponent;
		componentData.m_IndustrialUpkeepLevelExponent = m_IndustrialUpkeepLevelExponent;
		componentData.m_PerOfficeResourceNeededForIndustrial = m_PerOfficeResourceNeededForIndustrial;
		componentData.m_UnemploymentAllowanceMaxDays = m_UnemploymentAllowanceMaxDays;
		componentData.m_ShopPossibilityIncreaseDivider = m_ShopPossibilityIncreaseDivider;
		componentData.m_PlayerStartMoney = m_PlayerStartMoney;
		componentData.m_BuildRefundPercentage = m_BuildRefundPercentage;
		componentData.m_BuildRefundTimeRange = m_BuildRefundTimeRange;
		componentData.m_RelocationCostMultiplier = m_RelocationCostMultiplierOverride;
		componentData.m_RoadRefundPercentage = m_RoadRefundPercentage;
		componentData.m_RoadRefundTimeRange = m_RoadRefundTimeRange;
		componentData.m_TreeCostMultipliers = m_TreeCostMultipliers;
		componentData.m_MapTileUpkeepCostMultiplier = new AnimationCurve1(m_MapTileUpkeepCostMultiplier);
		componentData.m_LoanMinMaxInterestRate = m_LoanMinMaxInterestRate;
		entityManager.SetComponentData(singletonEntity, componentData);
		return deps;
	}

	public override void RestoreDefaultData(EntityManager entityManager, ref NativeArray<Entity> entities, PrefabSystem prefabSystem)
	{
		Entity entity = entities[0];
		EconomyPrefab economyPrefab = prefabSystem.GetPrefab<EconomyPrefab>(entity);
		EconomyParameterData componentData = entityManager.GetComponentData<EconomyParameterData>(entity);
		componentData.m_ExtractorCompanyExportMultiplier = economyPrefab.m_ExtractorCompanyExportMultiplier;
		componentData.m_Wage0 = economyPrefab.m_Wage0;
		componentData.m_Wage1 = economyPrefab.m_Wage1;
		componentData.m_Wage2 = economyPrefab.m_Wage2;
		componentData.m_Wage3 = economyPrefab.m_Wage3;
		componentData.m_Wage4 = economyPrefab.m_Wage4;
		componentData.m_CommuterWageMultiplier = economyPrefab.m_CommuterWageMultiplier;
		componentData.m_CityServiceWageAdjustment = economyPrefab.m_CityServiceWageAdjustment;
		componentData.m_CompanyBankruptcyLimit = economyPrefab.m_CompanyBankruptcyLimit;
		componentData.m_ResidentialMinimumEarnings = economyPrefab.m_ResidentialMinimumEarnings;
		componentData.m_UnemploymentBenefit = economyPrefab.m_UnemploymentBenefit;
		componentData.m_Pension = economyPrefab.m_Pension;
		componentData.m_FamilyAllowance = economyPrefab.m_FamilyAllowance;
		componentData.m_ResourceConsumptionMultiplier = economyPrefab.m_ResourceConsumptionMultiplier;
		componentData.m_ResourceConsumptionPerCitizen = economyPrefab.m_ResourceConsumptionPerCitizen;
		componentData.m_TouristConsumptionMultiplier = economyPrefab.m_TouristConsumptionMultiplier;
		componentData.m_WorkDayStart = economyPrefab.m_WorkDayStart;
		componentData.m_WorkDayEnd = economyPrefab.m_WorkDayEnd;
		componentData.m_IndustrialEfficiency = economyPrefab.m_IndustrialEfficiency;
		componentData.m_CommercialEfficiency = economyPrefab.m_CommercialEfficiency;
		componentData.m_ExtractorProductionEfficiency = economyPrefab.m_ExtractorEfficiency;
		componentData.m_TrafficReduction = economyPrefab.m_TrafficReduction;
		componentData.m_MaxCitySpecializationBonus = economyPrefab.m_MaxCitySpecializationBonus;
		componentData.m_ResourceProductionCoefficient = economyPrefab.m_ResourceProductionCoefficient;
		componentData.m_MixedBuildingCompanyRentPercentage = economyPrefab.m_MixedBuildingCompanyRentPercentage;
		componentData.m_LandValueModifier = economyPrefab.m_LandValueModifier;
		componentData.m_RentPriceBuildingZoneTypeBase = economyPrefab.m_RentPriceBuildingZoneTypeBase;
		componentData.m_ResidentialUpkeepLevelExponent = economyPrefab.m_ResidentialUpkeepLevelExponent;
		componentData.m_CommercialUpkeepLevelExponent = economyPrefab.m_CommercialUpkeepLevelExponent;
		componentData.m_IndustrialUpkeepLevelExponent = economyPrefab.m_IndustrialUpkeepLevelExponent;
		componentData.m_PerOfficeResourceNeededForIndustrial = economyPrefab.m_PerOfficeResourceNeededForIndustrial;
		componentData.m_UnemploymentAllowanceMaxDays = economyPrefab.m_UnemploymentAllowanceMaxDays;
		componentData.m_ShopPossibilityIncreaseDivider = economyPrefab.m_ShopPossibilityIncreaseDivider;
		componentData.m_PlayerStartMoney = economyPrefab.m_PlayerStartMoney;
		componentData.m_BuildRefundPercentage = economyPrefab.m_BuildRefundPercentage;
		componentData.m_BuildRefundTimeRange = economyPrefab.m_BuildRefundTimeRange;
		componentData.m_RelocationCostMultiplier = economyPrefab.m_RelocationCostMultiplier;
		componentData.m_RoadRefundPercentage = economyPrefab.m_RoadRefundPercentage;
		componentData.m_RoadRefundTimeRange = economyPrefab.m_RoadRefundTimeRange;
		componentData.m_TreeCostMultipliers = economyPrefab.m_TreeCostMultipliers;
		componentData.m_MapTileUpkeepCostMultiplier = new AnimationCurve1(economyPrefab.m_MapTileUpkeepCostMultiplier);
		componentData.m_LoanMinMaxInterestRate = economyPrefab.m_LoanMinMaxInterestRate;
		entityManager.SetComponentData(entity, componentData);
	}
}
