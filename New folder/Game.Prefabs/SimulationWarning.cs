using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Notifications/", new Type[] { typeof(NotificationIconPrefab) })]
public class SimulationWarning : ComponentBase
{
	public IconCategory[] m_Categories;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (m_Categories != null)
		{
			NotificationIconDisplayData componentData = entityManager.GetComponentData<NotificationIconDisplayData>(entity);
			for (int i = 0; i < m_Categories.Length; i++)
			{
				componentData.m_CategoryMask |= (uint)(1 << (int)m_Categories[i]);
			}
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
