using System;
using System.Collections.Generic;
using Game.Areas;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Areas/", new Type[]
{
	typeof(LotPrefab),
	typeof(SpacePrefab)
})]
public class HangaroundArea : ComponentBase
{
	public ActivityType[] m_Activities;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<SpawnLocationData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<HangaroundLocation>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		NavigationArea component = GetComponent<NavigationArea>();
		SpawnLocationData componentData = default(SpawnLocationData);
		componentData.m_ConnectionType = component.m_ConnectionType;
		componentData.m_TrackTypes = component.m_TrackTypes;
		componentData.m_RoadTypes = component.m_RoadTypes;
		componentData.m_RequireAuthorization = false;
		componentData.m_HangaroundOnLane = false;
		if (m_Activities != null && m_Activities.Length != 0)
		{
			componentData.m_ActivityMask = default(ActivityMask);
			for (int i = 0; i < m_Activities.Length; i++)
			{
				componentData.m_ActivityMask.m_Mask |= new ActivityMask(m_Activities[i]).m_Mask;
			}
		}
		else
		{
			componentData.m_ActivityMask = new ActivityMask(ActivityType.Standing);
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
