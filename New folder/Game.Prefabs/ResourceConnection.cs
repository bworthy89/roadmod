using System;
using System.Collections.Generic;
using Game.Economy;
using Game.Net;
using Game.Objects;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[]
{
	typeof(NetPrefab),
	typeof(ObjectPrefab)
})]
public class ResourceConnection : ComponentBase
{
	public ResourceInEditor m_Resource;

	public NotificationIconPrefab m_ConnectionWarningNotification;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_ConnectionWarningNotification != null)
		{
			prefabs.Add(m_ConnectionWarningNotification);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ResourceConnectionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (components.Contains(ComponentType.ReadWrite<Node>()))
		{
			components.Add(ComponentType.ReadWrite<Game.Net.ResourceConnection>());
		}
		else if (components.Contains(ComponentType.ReadWrite<Edge>()))
		{
			components.Add(ComponentType.ReadWrite<Game.Net.ResourceConnection>());
		}
		else if (components.Contains(ComponentType.ReadWrite<Game.Objects.Object>()))
		{
			components.Add(ComponentType.ReadWrite<Game.Net.ResourceConnection>());
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		ResourceConnectionData componentData = new ResourceConnectionData
		{
			m_Resource = EconomyUtils.GetResource(m_Resource)
		};
		if (m_ConnectionWarningNotification != null)
		{
			componentData.m_ConnectionWarningNotification = orCreateSystemManaged.GetEntity(m_ConnectionWarningNotification);
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
