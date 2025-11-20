using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Services/", new Type[]
{
	typeof(ServicePrefab),
	typeof(ZonePrefab)
})]
public class CrimeAccumulation : ComponentBase
{
	public float m_CrimeRate = 7f;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<CrimeAccumulationData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		CrimeAccumulationData componentData = default(CrimeAccumulationData);
		componentData.m_CrimeRate = m_CrimeRate;
		entityManager.SetComponentData(entity, componentData);
	}
}
