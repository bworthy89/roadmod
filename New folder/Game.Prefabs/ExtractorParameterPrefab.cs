using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class ExtractorParameterPrefab : PrefabBase
{
	[Tooltip("How much of the fertility resource is consumed per produced agricultural unit")]
	public float m_FertilityConsumption = 0.1f;

	[Tooltip("How much of the fish resource is consumed per produced aquacultural unit")]
	public float m_FishConsumption = 0.1f;

	[Tooltip("How much ore can be extracted before efficiency drops to 1/2.71th of the original")]
	public float m_OreConsumption = 500000f;

	[Tooltip("How much of the forest resource is consumed per produced wood unit")]
	public float m_ForestConsumption = 1f;

	[Tooltip("How much oil can be extracted before efficiency drops to 1/2.71th of the original")]
	public float m_OilConsumption = 100000f;

	[Tooltip("If the resource concentration goes under this limit, the productivity starts to drop")]
	public float m_FullFertility = 0.5f;

	[Tooltip("If the resource concentration goes under this limit, the productivity starts to drop")]
	public float m_FullFish = 0.5f;

	[Tooltip("If the resource concentration goes under this limit, the productivity starts to drop")]
	public float m_FullOre = 0.8f;

	[Tooltip("If the resource concentration goes under this limit, the productivity starts to drop")]
	public float m_FullOil = 0.8f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ExtractorParameterData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		entityManager.SetComponentData(entity, new ExtractorParameterData
		{
			m_FertilityConsumption = m_FertilityConsumption,
			m_FishConsumption = m_FishConsumption,
			m_ForestConsumption = m_ForestConsumption,
			m_OreConsumption = m_OreConsumption,
			m_OilConsumption = m_OilConsumption,
			m_FullFertility = m_FullFertility,
			m_FullFish = m_FullFish,
			m_FullOil = m_FullOil,
			m_FullOre = m_FullOre
		});
	}
}
