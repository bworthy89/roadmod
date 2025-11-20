using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Services/", new Type[]
{
	typeof(ObjectPrefab),
	typeof(NetPrefab),
	typeof(AreaPrefab),
	typeof(ZonePrefab),
	typeof(RoutePrefab),
	typeof(TerraformingPrefab)
})]
public class ServiceObject : ComponentBase
{
	public ServicePrefab m_Service;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ServiceObjectData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		entityManager.SetComponentData(entity, new ServiceObjectData
		{
			m_Service = existingSystemManaged.GetEntity(m_Service)
		});
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_Service);
	}
}
