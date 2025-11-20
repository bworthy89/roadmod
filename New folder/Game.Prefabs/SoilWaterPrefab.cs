using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class SoilWaterPrefab : PrefabBase
{
	[Tooltip("Amount of water full rain adds to soil simulation")]
	public float m_RainMultiplier = 16f;

	[Tooltip("How much terrain height affects where the soil water flows")]
	public float m_HeightEffect = 0.1f;

	[Tooltip("Maximum portion of soil wetness can change per update (high values unstable)")]
	public float m_MaxDiffusion = 0.05f;

	[Tooltip("How much surface water does a full cell of soil water correspond to")]
	public float m_WaterPerUnit = 0.1f;

	[Tooltip("Target soil moisture when land is underwater")]
	public float m_MoistureUnderWater = 0.5f;

	[Tooltip("What water depth counts as fully underwater for soil moisture")]
	public float m_MaximumWaterDepth = 10f;

	[Tooltip("What portion of extra moisture is transformed into surface water per update")]
	public float m_OverflowRate = 0.1f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<SoilWaterParameterData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		entityManager.SetComponentData(entity, new SoilWaterParameterData
		{
			m_RainMultiplier = m_RainMultiplier,
			m_HeightEffect = m_HeightEffect,
			m_MaxDiffusion = m_MaxDiffusion,
			m_WaterPerUnit = m_WaterPerUnit,
			m_MoistureUnderWater = m_MoistureUnderWater,
			m_MaximumWaterDepth = m_MaximumWaterDepth,
			m_OverflowRate = m_OverflowRate
		});
	}
}
