using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Notifications/", new Type[] { typeof(NotificationIconPrefab) })]
public class BuildingMarker : ComponentBase
{
	public BuildingType m_BuildingType;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<BuildingMarkerData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		BuildingMarkerData componentData = default(BuildingMarkerData);
		componentData.m_BuildingType = m_BuildingType;
		entityManager.SetComponentData(entity, componentData);
	}
}
