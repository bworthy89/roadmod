using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Economy;
using Game.Vehicles;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Areas/", new Type[] { typeof(LotPrefab) })]
public class StorageArea : ComponentBase
{
	public ResourceInEditor[] m_StoredResources;

	public int m_Capacity;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<StorageAreaData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Storage>());
		components.Add(ComponentType.ReadWrite<OwnedVehicle>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		Resource resource = Resource.NoResource;
		if (m_StoredResources != null)
		{
			for (int i = 0; i < m_StoredResources.Length; i++)
			{
				resource |= EconomyUtils.GetResource(m_StoredResources[i]);
			}
		}
		StorageAreaData componentData = default(StorageAreaData);
		componentData.m_Resources = resource;
		componentData.m_Capacity = m_Capacity;
		entityManager.SetComponentData(entity, componentData);
	}
}
