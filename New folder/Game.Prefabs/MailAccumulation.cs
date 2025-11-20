using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Services/", new Type[]
{
	typeof(ServicePrefab),
	typeof(ZonePrefab)
})]
public class MailAccumulation : ComponentBase
{
	public bool m_RequireCollect;

	public float m_SendingRate = 1f;

	public float m_ReceivingRate = 1f;

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<MailAccumulationData>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		MailAccumulationData componentData = default(MailAccumulationData);
		componentData.m_RequireCollect = m_RequireCollect;
		componentData.m_AccumulationRate.x = m_SendingRate;
		componentData.m_AccumulationRate.y = m_ReceivingRate;
		entityManager.SetComponentData(entity, componentData);
	}
}
