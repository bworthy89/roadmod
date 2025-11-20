using System;
using System.Collections.Generic;
using Game.Agents;
using Game.Citizens;
using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Citizens/", new Type[] { })]
public class CitizenPrefab : ArchetypePrefab
{
	public bool m_Male;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<CitizenData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<Citizen>());
		components.Add(ComponentType.ReadWrite<TripNeeded>());
		components.Add(ComponentType.ReadWrite<CrimeVictim>());
		components.Add(ComponentType.ReadWrite<MailSender>());
		components.Add(ComponentType.ReadWrite<Arrived>());
		components.Add(ComponentType.ReadWrite<CarKeeper>());
		components.Add(ComponentType.ReadWrite<BicycleOwner>());
		components.Add(ComponentType.ReadWrite<HasJobSeeker>());
		components.Add(ComponentType.ReadWrite<UpdateFrame>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new CitizenData
		{
			m_Male = m_Male
		});
	}
}
