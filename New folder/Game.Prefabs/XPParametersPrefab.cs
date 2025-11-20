using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class XPParametersPrefab : PrefabBase
{
	public float m_XPPerPopulation;

	public float m_XPPerHappiness;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<XPParameterData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		entityManager.SetComponentData(entity, new XPParameterData
		{
			m_XPPerHappiness = m_XPPerHappiness,
			m_XPPerPopulation = m_XPPerPopulation
		});
	}
}
