using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class TrafficConfigurationPrefab : PrefabBase
{
	public NotificationIconPrefab m_BottleneckNotification;

	public NotificationIconPrefab m_DeadEndNotification;

	public NotificationIconPrefab m_RoadConnectionNotification;

	public NotificationIconPrefab m_TrackConnectionNotification;

	public NotificationIconPrefab m_CarConnectionNotification;

	public NotificationIconPrefab m_ShipConnectionNotification;

	public NotificationIconPrefab m_TrainConnectionNotification;

	public NotificationIconPrefab m_PedestrianConnectionNotification;

	public NotificationIconPrefab m_BicycleConnectionNotification;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_BottleneckNotification);
		prefabs.Add(m_DeadEndNotification);
		prefabs.Add(m_RoadConnectionNotification);
		prefabs.Add(m_TrackConnectionNotification);
		prefabs.Add(m_CarConnectionNotification);
		prefabs.Add(m_ShipConnectionNotification);
		prefabs.Add(m_TrainConnectionNotification);
		prefabs.Add(m_PedestrianConnectionNotification);
		prefabs.Add(m_BicycleConnectionNotification);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TrafficConfigurationData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		entityManager.SetComponentData(entity, new TrafficConfigurationData
		{
			m_BottleneckNotification = orCreateSystemManaged.GetEntity(m_BottleneckNotification),
			m_DeadEndNotification = orCreateSystemManaged.GetEntity(m_DeadEndNotification),
			m_RoadConnectionNotification = orCreateSystemManaged.GetEntity(m_RoadConnectionNotification),
			m_TrackConnectionNotification = orCreateSystemManaged.GetEntity(m_TrackConnectionNotification),
			m_CarConnectionNotification = orCreateSystemManaged.GetEntity(m_CarConnectionNotification),
			m_ShipConnectionNotification = orCreateSystemManaged.GetEntity(m_ShipConnectionNotification),
			m_TrainConnectionNotification = orCreateSystemManaged.GetEntity(m_TrainConnectionNotification),
			m_PedestrianConnectionNotification = orCreateSystemManaged.GetEntity(m_PedestrianConnectionNotification),
			m_BicycleConnectionNotification = orCreateSystemManaged.GetEntity(m_BicycleConnectionNotification)
		});
	}
}
