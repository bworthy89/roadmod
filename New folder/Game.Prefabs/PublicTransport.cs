using System;
using System.Collections.Generic;
using Game.Objects;
using Game.Pathfind;
using Game.PSI;
using Game.Simulation;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ExcludeGeneratedModTag]
[ComponentMenu("Vehicles/", new Type[] { typeof(VehiclePrefab) })]
public class PublicTransport : ComponentBase
{
	public TransportType m_TransportType;

	public int m_PassengerCapacity = 30;

	[EnumFlag]
	public PublicTransportPurpose m_Purposes = PublicTransportPurpose.TransportLine;

	public float m_MaintenanceRange;

	public override IEnumerable<string> modTags
	{
		get
		{
			foreach (string modTag in base.modTags)
			{
				yield return modTag;
			}
			if ((m_Purposes & PublicTransportPurpose.TransportLine) != 0)
			{
				yield return "PublicTransport";
				yield return "PublicTransport" + m_TransportType;
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PublicTransportVehicleData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Vehicles.PublicTransport>());
		components.Add(ComponentType.ReadWrite<Passenger>());
		components.Add(ComponentType.ReadWrite<Odometer>());
		if (components.Contains(ComponentType.ReadWrite<Moving>()) && (!components.Contains(ComponentType.ReadWrite<Controller>()) || components.Contains(ComponentType.ReadWrite<LayoutElement>())))
		{
			components.Add(ComponentType.ReadWrite<PathInformation>());
			components.Add(ComponentType.ReadWrite<ServiceDispatch>());
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new PublicTransportVehicleData(m_TransportType, m_PassengerCapacity, m_Purposes, m_MaintenanceRange * 1000f));
		if (entityManager.HasComponent<CarData>(entity))
		{
			entityManager.SetComponentData(entity, new UpdateFrameData(1));
		}
	}
}
