using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[]
{
	typeof(StaticObjectPrefab),
	typeof(MarkerObjectPrefab)
})]
public class TrafficSignObject : ComponentBase
{
	public TrafficSignType[] m_SignTypes;

	public int m_SpeedLimit;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<TrafficSignData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		TrafficSignData componentData = default(TrafficSignData);
		componentData.m_TypeMask = 0u;
		componentData.m_SpeedLimit = m_SpeedLimit;
		if (m_SignTypes != null)
		{
			for (int i = 0; i < m_SignTypes.Length; i++)
			{
				componentData.m_TypeMask |= TrafficSignData.GetTypeMask(m_SignTypes[i]);
			}
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
