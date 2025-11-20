using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Notifications/", new Type[] { typeof(NotificationIconPrefab) })]
public class VehicleMarker : ComponentBase
{
	public VehicleType m_VehicleType;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<VehicleMarkerData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		VehicleMarkerData componentData = default(VehicleMarkerData);
		componentData.m_VehicleType = m_VehicleType;
		entityManager.SetComponentData(entity, componentData);
	}
}
