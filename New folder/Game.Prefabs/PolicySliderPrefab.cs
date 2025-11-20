using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Policies/", new Type[] { })]
public class PolicySliderPrefab : PolicyPrefab
{
	public Bounds1 m_SliderRange = new Bounds1(0f, 1f);

	public float m_SliderDefault = 0.5f;

	public float m_SliderStep = 0.1f;

	public PolicySliderUnit m_Unit = PolicySliderUnit.integer;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<PolicySliderData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		PolicySliderData componentData = default(PolicySliderData);
		componentData.m_Range = m_SliderRange;
		componentData.m_Default = m_SliderDefault;
		componentData.m_Step = m_SliderStep;
		componentData.m_Unit = (int)m_Unit;
		entityManager.SetComponentData(entity, componentData);
	}
}
