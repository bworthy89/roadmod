using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Economy;
using Game.Pathfind;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Vehicles/", new Type[]
{
	typeof(CarPrefab),
	typeof(CarTrailerPrefab),
	typeof(WatercraftPrefab)
})]
public class WorkVehicle : ComponentBase
{
	public VehicleWorkType m_WorkType;

	public MapFeature m_MapFeature = MapFeature.None;

	public ResourceInEditor[] m_Resources;

	public float m_MaxWorkAmount = 30000f;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<WorkVehicleData>());
		components.Add(ComponentType.ReadWrite<UpdateFrameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Vehicles.WorkVehicle>());
		components.Add(ComponentType.ReadWrite<PathInformation>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		Resource resource = Resource.NoResource;
		if (m_Resources != null)
		{
			for (int i = 0; i < m_Resources.Length; i++)
			{
				resource |= EconomyUtils.GetResource(m_Resources[i]);
			}
		}
		WorkVehicleData componentData = default(WorkVehicleData);
		componentData.m_WorkType = m_WorkType;
		componentData.m_MapFeature = m_MapFeature;
		componentData.m_MaxWorkAmount = m_MaxWorkAmount;
		componentData.m_Resources = resource;
		entityManager.SetComponentData(entity, componentData);
		if (entityManager.HasComponent<CarData>(entity))
		{
			entityManager.SetComponentData(entity, new UpdateFrameData(12));
		}
	}
}
