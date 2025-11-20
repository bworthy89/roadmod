using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Policies/", new Type[] { typeof(PolicyPrefab) })]
public class RouteModifiers : ComponentBase
{
	public RouteModifierInfo[] m_Modifiers;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<RouteModifierData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (m_Modifiers != null)
		{
			DynamicBuffer<RouteModifierData> buffer = entityManager.GetBuffer<RouteModifierData>(entity);
			for (int i = 0; i < m_Modifiers.Length; i++)
			{
				RouteModifierInfo routeModifierInfo = m_Modifiers[i];
				buffer.Add(new RouteModifierData(routeModifierInfo.m_Type, routeModifierInfo.m_Mode, routeModifierInfo.m_Range));
			}
		}
	}
}
