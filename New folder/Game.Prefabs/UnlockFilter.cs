using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Unlocking/", new Type[] { typeof(BuildingPrefab) })]
public class UnlockFilter : ComponentBase
{
	public int m_UnlockUniqueID;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<UnlockFilterData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		UnlockFilterData componentData = new UnlockFilterData
		{
			m_UnlockUniqueID = m_UnlockUniqueID
		};
		entityManager.SetComponentData(entity, componentData);
	}
}
