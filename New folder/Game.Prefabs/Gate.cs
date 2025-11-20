using System;
using System.Collections.Generic;
using Game.Routes;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[] { typeof(BuildingPrefab) })]
public class Gate : ComponentBase
{
	public VerifiedPathPrefab m_BypassPathPrefab;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_BypassPathPrefab);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<GateData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (GetComponent<ServiceUpgrade>() == null)
		{
			components.Add(ComponentType.ReadWrite<SubRoute>());
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		GateData componentData = default(GateData);
		componentData.m_BypassPathPrefab = existingSystemManaged.GetEntity(m_BypassPathPrefab);
		entityManager.SetComponentData(entity, componentData);
	}
}
