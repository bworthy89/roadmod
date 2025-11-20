using System;
using System.Collections.Generic;
using Game.Citizens;
using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Citizens/", new Type[] { })]
public class HouseholdPetPrefab : ArchetypePrefab
{
	public PetType m_Type;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<HouseholdPetData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<HouseholdPet>());
		components.Add(ComponentType.ReadWrite<UpdateFrame>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		HouseholdPetData componentData = default(HouseholdPetData);
		componentData.m_Type = m_Type;
		entityManager.SetComponentData(entity, componentData);
	}
}
