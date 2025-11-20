using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Creatures/", new Type[] { typeof(AnimalPrefab) })]
public class FlyingAnimal : ComponentBase
{
	public float m_FlySpeed = 100f;

	public Bounds1 m_FlyHeight = new Bounds1(20f, 100f);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		AnimalData componentData = entityManager.GetComponentData<AnimalData>(entity);
		componentData.m_FlySpeed = m_FlySpeed / 3.6f;
		componentData.m_FlyHeight = m_FlyHeight;
		entityManager.SetComponentData(entity, componentData);
	}
}
