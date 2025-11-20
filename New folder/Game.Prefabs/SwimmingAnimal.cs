using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Creatures/", new Type[] { typeof(AnimalPrefab) })]
public class SwimmingAnimal : ComponentBase
{
	public float m_SwimSpeed = 20f;

	public Bounds1 m_SwimDepth = new Bounds1(5f, 20f);

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
		componentData.m_SwimSpeed = m_SwimSpeed / 3.6f;
		componentData.m_SwimDepth = m_SwimDepth;
		entityManager.SetComponentData(entity, componentData);
	}
}
