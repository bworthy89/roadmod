using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class RouteConfigurationPrefab : PrefabBase
{
	public NotificationIconPrefab m_PathfindNotification;

	public NotificationIconPrefab m_GateBypassNotification;

	public RoutePrefab m_CarPathVisualization;

	public RoutePrefab m_WatercraftPathVisualization;

	public RoutePrefab m_AircraftPathVisualization;

	public RoutePrefab m_TrainPathVisualization;

	public RoutePrefab m_HumanPathVisualization;

	public RoutePrefab m_BicyclePathVisualization;

	public RoutePrefab m_MissingRoutePrefab;

	public float m_GateBypassEfficiency = -0.5f;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_PathfindNotification);
		prefabs.Add(m_GateBypassNotification);
		prefabs.Add(m_CarPathVisualization);
		prefabs.Add(m_WatercraftPathVisualization);
		prefabs.Add(m_AircraftPathVisualization);
		prefabs.Add(m_TrainPathVisualization);
		prefabs.Add(m_HumanPathVisualization);
		prefabs.Add(m_BicyclePathVisualization);
		prefabs.Add(m_MissingRoutePrefab);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<RouteConfigurationData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		entityManager.SetComponentData(entity, new RouteConfigurationData
		{
			m_PathfindNotification = orCreateSystemManaged.GetEntity(m_PathfindNotification),
			m_GateBypassNotification = orCreateSystemManaged.GetEntity(m_GateBypassNotification),
			m_CarPathVisualization = orCreateSystemManaged.GetEntity(m_CarPathVisualization),
			m_WatercraftPathVisualization = orCreateSystemManaged.GetEntity(m_WatercraftPathVisualization),
			m_AircraftPathVisualization = orCreateSystemManaged.GetEntity(m_AircraftPathVisualization),
			m_TrainPathVisualization = orCreateSystemManaged.GetEntity(m_TrainPathVisualization),
			m_HumanPathVisualization = orCreateSystemManaged.GetEntity(m_HumanPathVisualization),
			m_BicyclePathVisualization = orCreateSystemManaged.GetEntity(m_BicyclePathVisualization),
			m_MissingRoutePrefab = orCreateSystemManaged.GetEntity(m_MissingRoutePrefab),
			m_GateBypassEfficiency = m_GateBypassEfficiency
		});
	}
}
