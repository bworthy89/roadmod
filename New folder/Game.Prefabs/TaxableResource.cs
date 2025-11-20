using System;
using System.Collections.Generic;
using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Resources/", new Type[] { typeof(ResourcePrefab) })]
public class TaxableResource : ComponentBase
{
	public TaxAreaType[] m_TaxAreas;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TaxableResourceData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (m_TaxAreas != null)
		{
			entityManager.SetComponentData(entity, new TaxableResourceData(m_TaxAreas));
		}
	}
}
