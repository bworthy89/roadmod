using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class CompanyNotificationParameterPrefab : PrefabBase
{
	public NotificationIconPrefab m_NoInputsNotificationPrefab;

	public NotificationIconPrefab m_NoCustomersNotificationPrefab;

	public float m_NoInputCostLimit = 5f;

	public float m_NoCustomersServiceLimit = 0.9f;

	[Tooltip("The limit of empty rooms percentage of total room amount, 0.9 means 90% rooms are empty")]
	public float m_NoCustomersHotelLimit = 0.9f;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_NoInputsNotificationPrefab);
		prefabs.Add(m_NoCustomersNotificationPrefab);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<CompanyNotificationParameterData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		CompanyNotificationParameterData componentData = default(CompanyNotificationParameterData);
		componentData.m_NoCustomersNotificationPrefab = orCreateSystemManaged.GetEntity(m_NoCustomersNotificationPrefab);
		componentData.m_NoInputsNotificationPrefab = orCreateSystemManaged.GetEntity(m_NoInputsNotificationPrefab);
		componentData.m_NoCustomersServiceLimit = m_NoCustomersServiceLimit;
		componentData.m_NoInputCostLimit = m_NoInputCostLimit;
		componentData.m_NoCustomersHotelLimit = m_NoCustomersHotelLimit;
		entityManager.SetComponentData(entity, componentData);
	}
}
