using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/", new Type[] { })]
public class ModeSetting : PrefabBase
{
	[Tooltip("Toggle to enable all of the systems below, does not affect the enable state of global and local mode prefabs")]
	public bool m_Enable;

	[Header("System value modification")]
	[Tooltip("Weights for residential demand factors. use x for negative and y for positive factors, Default is (1,1)")]
	public float2 m_ResidentialDemandWeightsSelector;

	[Tooltip("Offset for commercial demand based on tax factor. Tax factor range [-1,1]")]
	public float m_CommercialTaxEffectDemandOffset;

	[Tooltip("Offset for industrial and office demand based on tax factor. Tax factor is range [-1,1]")]
	public float m_IndustrialOfficeTaxEffectDemandOffset;

	[Tooltip("Multiplier for resource demand per citizen, Default is 1")]
	public float m_ResourceDemandPerCitizenMultiplier;

	[Tooltip("Multiply the amount of tax paid by citizens (x), commercial (y), industrial+office (z), Default is (1,1,1). Does not affect player income and city statistics")]
	public float3 m_TaxPaidMultiplier;

	[Header("Give money to poor citizens")]
	[Tooltip("Should give more support to poor citizens?")]
	public bool m_SupportPoorCitizens;

	[Tooltip("If citizen wealth is below this value, they will be give money to reach this value")]
	public int m_MinimumWealth;

	[Header("Government subsidies")]
	[Tooltip("Should enable government subsidies?")]
	public bool m_EnableGovernmentSubsidies;

	[Tooltip("Player money level that government subsidies kick in (x) and max cover threshold (y)")]
	public int2 m_MoneyCoverThreshold;

	[Tooltip("Maximum percentage of expenses that government subsidies cover. (0 mean no cover, 100 mean full cover, 200 mean double cover the expenses)")]
	public int m_MaxMoneyCoverPercentage;

	[Header("Natural resources adjustment: (Groundwater, Fertility, Ore, Oil)")]
	public bool m_EnableAdjustNaturalResources;

	[Tooltip("Initial multiplier for natural resources: 0 mean no resources, 1 mean normal resources")]
	public float m_InitialNaturalResourceBoostMultiplier;

	[Tooltip("Percentage of ore refill amount per day, 0 mean no refill, 100 mean full refill after 1 day")]
	public int m_PercentOreRefillAmountPerDay;

	[Tooltip("Percentage of oil refill amount per day, 0 mean no refill, 100 mean full refill after 1 day")]
	public int m_PercentOilRefillAmountPerDay;

	public List<ModePrefab> m_ModePrefabs;

	private List<LocalModePrefab> m_LocalModePrefabs;

	private List<EntityQueryModePrefab> m_GlobalModePrefabs;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<GameModeSettingData>());
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		if (m_ModePrefabs != null)
		{
			for (int i = 0; i < m_ModePrefabs.Count; i++)
			{
				prefabs.Add(m_ModePrefabs[i]);
			}
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		m_LocalModePrefabs = new List<LocalModePrefab>();
		m_GlobalModePrefabs = new List<EntityQueryModePrefab>();
		if (m_ModePrefabs == null)
		{
			return;
		}
		for (int i = 0; i < m_ModePrefabs.Count; i++)
		{
			if (m_ModePrefabs[i] is LocalModePrefab item)
			{
				m_LocalModePrefabs.Add(item);
			}
			else if (m_ModePrefabs[i] is EntityQueryModePrefab item2)
			{
				m_GlobalModePrefabs.Add(item2);
			}
		}
	}

	public void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		Entity singletonEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ModeSettingData>()).GetSingletonEntity();
		entityManager.SetComponentData(singletonEntity, new ModeSettingData
		{
			m_Enable = false
		});
		for (int i = 0; i < m_LocalModePrefabs.Count; i++)
		{
			m_LocalModePrefabs[i].RestoreDefaultData(entityManager, prefabSystem);
		}
		for (int j = 0; j < m_GlobalModePrefabs.Count; j++)
		{
			EntityQuery entityQuery = entityManager.CreateEntityQuery(m_GlobalModePrefabs[j].GetEntityQueryDesc());
			if (!entityQuery.IsEmptyIgnoreFilter)
			{
				NativeArray<Entity> entities = entityQuery.ToEntityArray(Allocator.TempJob);
				m_GlobalModePrefabs[j].RestoreDefaultData(entityManager, ref entities, prefabSystem);
				entities.Dispose();
			}
		}
	}

	public void StoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_GlobalModePrefabs.Count; i++)
		{
			EntityQuery entityQuery = entityManager.CreateEntityQuery(m_GlobalModePrefabs[i].GetEntityQueryDesc());
			if (!entityQuery.IsEmptyIgnoreFilter)
			{
				NativeArray<Entity> requestedQuery = entityQuery.ToEntityArray(Allocator.TempJob);
				m_GlobalModePrefabs[i].StoreDefaultData(entityManager, ref requestedQuery, prefabSystem);
				requestedQuery.Dispose();
			}
		}
	}

	public JobHandle ApplyMode(EntityManager entityManager, PrefabSystem prefabSystem, JobHandle deps)
	{
		Entity singletonEntity = entityManager.CreateEntityQuery(ComponentType.ReadOnly<ModeSettingData>()).GetSingletonEntity();
		ModeSettingData componentData = new ModeSettingData
		{
			m_Enable = m_Enable,
			m_ResidentialDemandWeightsSelector = m_ResidentialDemandWeightsSelector,
			m_CommercialTaxEffectDemandOffset = m_CommercialTaxEffectDemandOffset,
			m_IndustrialOfficeTaxEffectDemandOffset = m_IndustrialOfficeTaxEffectDemandOffset,
			m_ResourceDemandPerCitizenMultiplier = m_ResourceDemandPerCitizenMultiplier,
			m_TaxPaidMultiplier = m_TaxPaidMultiplier,
			m_SupportPoorCitizens = m_SupportPoorCitizens,
			m_MinimumWealth = m_MinimumWealth,
			m_EnableGovernmentSubsidies = m_EnableGovernmentSubsidies,
			m_MoneyCoverThreshold = m_MoneyCoverThreshold,
			m_MaxMoneyCoverPercentage = m_MaxMoneyCoverPercentage,
			m_EnableAdjustNaturalResources = m_EnableAdjustNaturalResources,
			m_InitialNaturalResourceBoostMultiplier = m_InitialNaturalResourceBoostMultiplier,
			m_PercentOreRefillAmountPerDay = m_PercentOreRefillAmountPerDay,
			m_PercentOilRefillAmountPerDay = m_PercentOilRefillAmountPerDay
		};
		entityManager.SetComponentData(singletonEntity, componentData);
		for (int i = 0; i < m_LocalModePrefabs.Count; i++)
		{
			m_LocalModePrefabs[i].ApplyModeData(entityManager, prefabSystem);
		}
		for (int j = 0; j < m_GlobalModePrefabs.Count; j++)
		{
			EntityQuery requestedQuery = entityManager.CreateEntityQuery(m_GlobalModePrefabs[j].GetEntityQueryDesc());
			if (!requestedQuery.IsEmptyIgnoreFilter)
			{
				deps = m_GlobalModePrefabs[j].ApplyModeData(entityManager, requestedQuery, deps);
			}
		}
		return deps;
	}

	public virtual void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_LocalModePrefabs.Count; i++)
		{
		}
		for (int j = 0; j < m_GlobalModePrefabs.Count; j++)
		{
			entityManager.CreateEntityQuery(m_GlobalModePrefabs[j].GetEntityQueryDesc()).ToEntityArray(Allocator.TempJob).Dispose();
		}
	}
}
