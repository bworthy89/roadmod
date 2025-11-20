using System;
using System.Collections.Generic;
using Game.Net;
using Game.Objects;
using Game.Routes;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[] { typeof(MarkerObjectPrefab) })]
public class SpawnLocation : ComponentBase
{
	public RouteConnectionType m_ConnectionType = RouteConnectionType.Pedestrian;

	public TrackTypes m_TrackTypes;

	public RoadTypes m_RoadTypes;

	public bool m_RequireAuthorization;

	public bool m_HangaroundOnLane;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<SpawnLocationData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Objects.SpawnLocation>());
		if (m_ConnectionType == RouteConnectionType.Air)
		{
			components.Add(ComponentType.ReadWrite<Game.Routes.TakeoffLocation>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		SpawnLocationData componentData = default(SpawnLocationData);
		componentData.m_ConnectionType = m_ConnectionType;
		componentData.m_ActivityMask = default(ActivityMask);
		componentData.m_TrackTypes = m_TrackTypes;
		componentData.m_RoadTypes = m_RoadTypes;
		componentData.m_RequireAuthorization = m_RequireAuthorization;
		componentData.m_HangaroundOnLane = m_HangaroundOnLane;
		entityManager.SetComponentData(entity, componentData);
	}
}
