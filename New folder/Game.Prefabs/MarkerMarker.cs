using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Notifications/", new Type[] { typeof(NotificationIconPrefab) })]
public class MarkerMarker : ComponentBase
{
	public MarkerType m_MarkerType;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<MarkerMarkerData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		MarkerMarkerData componentData = default(MarkerMarkerData);
		componentData.m_MarkerType = m_MarkerType;
		entityManager.SetComponentData(entity, componentData);
	}
}
