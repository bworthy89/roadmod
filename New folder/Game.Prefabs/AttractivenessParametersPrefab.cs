using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class AttractivenessParametersPrefab : PrefabBase
{
	public float m_ForestEffect = 10f;

	public float m_ForestDistance = 250f;

	public float m_ShoreEffect = 20f;

	public float m_ShoreDistance = 500f;

	[Tooltip("zero level in m, effect % per m, max effect %")]
	public float3 m_HeightBonus = new float3(50f, 0.02f, 10f);

	[Tooltip("Temperature that attract more tourists, between x and y, reach max effect when it's the middle of the temperature range")]
	public float2 m_AttractiveTemperature = new float2(18f, 26f);

	[Tooltip("Extreme temperature that reduce the tourism, below x or above y, reach max effect when there are 10 degree difference")]
	public float2 m_ExtremeTemperature = new float2(-10f, 30f);

	[Tooltip("Rain that affect tourism, precipitation x: start of punishment, y:max punishment to m_SnowRainExtremeAffect.y")]
	public float2 m_RainEffectRange = new float2(0.3f, 1f);

	[Tooltip("Snow that affect tourism, precipitation x: start of positive effect, y:max effect to m_SnowRainExtremeAffect.x")]
	public float2 m_SnowEffectRange = new float2(0.3f, 1f);

	[Tooltip("x:the attractiveness added to current when temperature inside of AttractiveTemperature, y:the attractiveness reduced from current when temperature inside of ExtremeTemperature")]
	public float2 m_TemperatureAffect = new float2(0.2f, -0.3f);

	[Tooltip("x:snowing weather affect, y:raining weather affect, z:extreme weather like storm,hail")]
	public float3 m_SnowRainExtremeAffect = new float3(0.3f, -0.1f, -0.3f);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<AttractivenessParameterData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.SetComponentData(entity, new AttractivenessParameterData
		{
			m_ForestDistance = m_ForestDistance,
			m_ForestEffect = m_ForestEffect,
			m_ShoreDistance = m_ShoreDistance,
			m_ShoreEffect = m_ShoreEffect,
			m_HeightBonus = m_HeightBonus,
			m_AttractiveTemperature = m_AttractiveTemperature,
			m_ExtremeTemperature = m_ExtremeTemperature,
			m_TemperatureAffect = m_TemperatureAffect,
			m_RainEffectRange = m_RainEffectRange,
			m_SnowEffectRange = m_SnowEffectRange,
			m_SnowRainExtremeAffect = m_SnowRainExtremeAffect
		});
	}
}
