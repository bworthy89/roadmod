using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Areas/", new Type[]
{
	typeof(LotPrefab),
	typeof(SpacePrefab)
})]
public class NavigationArea : ComponentBase
{
	public RouteConnectionType m_ConnectionType = RouteConnectionType.Pedestrian;

	public RouteConnectionType m_SecondaryType;

	public TrackTypes m_TrackTypes;

	public RoadTypes m_RoadTypes;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<NavigationAreaData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Navigation>());
		components.Add(ComponentType.ReadWrite<Game.Net.SubLane>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		NavigationAreaData componentData = default(NavigationAreaData);
		componentData.m_ConnectionType = m_ConnectionType;
		componentData.m_SecondaryType = m_SecondaryType;
		componentData.m_TrackTypes = m_TrackTypes;
		componentData.m_RoadTypes = m_RoadTypes;
		entityManager.SetComponentData(entity, componentData);
	}
}
