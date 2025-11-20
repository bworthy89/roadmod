using System;
using System.Collections.Generic;
using Game.Common;
using Game.Creatures;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Creatures/", new Type[] { })]
public class AnimalPrefab : CreaturePrefab
{
	public float m_MoveSpeed = 20f;

	public float m_Acceleration = 10f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<AnimalData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<Animal>());
		components.Add(ComponentType.ReadWrite<AnimalNavigation>());
		components.Add(ComponentType.ReadWrite<Target>());
		components.Add(ComponentType.ReadWrite<Blocker>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		AnimalData componentData = entityManager.GetComponentData<AnimalData>(entity);
		componentData.m_MoveSpeed = m_MoveSpeed / 3.6f;
		componentData.m_Acceleration = m_Acceleration;
		entityManager.SetComponentData(entity, componentData);
	}
}
