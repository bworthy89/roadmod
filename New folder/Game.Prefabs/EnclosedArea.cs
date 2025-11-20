using System;
using System.Collections.Generic;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Areas/", new Type[]
{
	typeof(LotPrefab),
	typeof(SpacePrefab),
	typeof(SurfacePrefab)
})]
public class EnclosedArea : ComponentBase
{
	public NetLanePrefab m_BorderLaneType;

	public bool m_CounterClockWise;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_BorderLaneType);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<EnclosedAreaData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Game.Net.SubLane>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		EnclosedAreaData componentData = default(EnclosedAreaData);
		componentData.m_BorderLanePrefab = existingSystemManaged.GetEntity(m_BorderLaneType);
		componentData.m_CounterClockWise = m_CounterClockWise;
		entityManager.SetComponentData(entity, componentData);
	}
}
