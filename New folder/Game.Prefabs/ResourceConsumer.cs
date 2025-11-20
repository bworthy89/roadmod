using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Game.Buildings;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
[RequireComponent(typeof(CityServiceBuilding))]
public class ResourceConsumer : ComponentBase
{
	[CanBeNull]
	public NotificationIconPrefab m_NoResourceNotificationPrefab;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_NoResourceNotificationPrefab != null)
		{
			prefabs.Add(m_NoResourceNotificationPrefab);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ResourceConsumerData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Buildings.ResourceConsumer>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		entityManager.SetComponentData(entity, new ResourceConsumerData
		{
			m_NoResourceNotificationPrefab = ((m_NoResourceNotificationPrefab != null) ? orCreateSystemManaged.GetEntity(m_NoResourceNotificationPrefab) : Entity.Null)
		});
	}
}
