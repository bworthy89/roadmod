using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Notifications/", new Type[] { typeof(NotificationIconPrefab) })]
public class TransportStopMarker : ComponentBase
{
	public TransportType m_TransportType;

	public bool m_PassengerTransport;

	public bool m_CargoTransport;

	public bool m_WorkStop;

	public bool m_WorkLocation;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TransportStopMarkerData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		TransportStopMarkerData componentData = default(TransportStopMarkerData);
		componentData.m_TransportType = m_TransportType;
		if (m_TransportType == TransportType.Work)
		{
			componentData.m_StopTypeA = m_WorkStop;
			componentData.m_StopTypeB = m_WorkLocation;
		}
		else
		{
			componentData.m_StopTypeA = m_PassengerTransport;
			componentData.m_StopTypeB = m_CargoTransport;
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
