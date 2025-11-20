using System;
using System.Collections.Generic;
using Game.Common;
using Game.Creatures;
using Game.Pathfind;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Creatures/", new Type[] { })]
public class HumanPrefab : CreaturePrefab
{
	public float m_WalkSpeed = 6f;

	public float m_RunSpeed = 12f;

	public float m_Acceleration = 8f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<HumanData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<Human>());
		components.Add(ComponentType.ReadWrite<HumanNavigation>());
		components.Add(ComponentType.ReadWrite<Queue>());
		components.Add(ComponentType.ReadWrite<PathOwner>());
		components.Add(ComponentType.ReadWrite<PathElement>());
		components.Add(ComponentType.ReadWrite<Target>());
		components.Add(ComponentType.ReadWrite<Blocker>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new HumanData
		{
			m_WalkSpeed = m_WalkSpeed / 3.6f,
			m_RunSpeed = m_RunSpeed / 3.6f,
			m_Acceleration = m_Acceleration
		});
	}
}
