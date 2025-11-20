using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[] { })]
public class ActivityLocationPrefab : TransformPrefab
{
	public ActivityType[] m_Activities;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ActivityLocationData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		ActivityLocationData componentData = default(ActivityLocationData);
		componentData.m_ActivityMask = default(ActivityMask);
		if (m_Activities != null)
		{
			for (int i = 0; i < m_Activities.Length; i++)
			{
				componentData.m_ActivityMask.m_Mask |= new ActivityMask(m_Activities[i]).m_Mask;
			}
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
