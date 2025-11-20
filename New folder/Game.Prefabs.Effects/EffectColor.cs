using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs.Effects;

[ComponentMenu("Effects/", new Type[] { typeof(EffectPrefab) })]
public class EffectColor : ComponentBase
{
	public EffectColorSource m_Source;

	[Range(0f, 100f)]
	public float m_HueRandomness;

	[Range(0f, 100f)]
	public float m_SaturationRandomness;

	[Range(0f, 100f)]
	public float m_BrightnessRandomness;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<EffectColorData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		EffectColorData componentData = entityManager.GetComponentData<EffectColorData>(entity);
		componentData.m_Source = m_Source;
		componentData.m_VaritationRanges.x = m_HueRandomness * 0.01f;
		componentData.m_VaritationRanges.y = m_SaturationRandomness * 0.01f;
		componentData.m_VaritationRanges.z = m_BrightnessRandomness * 0.01f;
		entityManager.SetComponentData(entity, componentData);
	}
}
