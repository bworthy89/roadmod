using System;
using System.Collections.Generic;
using Game.Policies;
using Game.Routes;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Policies/", new Type[]
{
	typeof(DistrictPrefab),
	typeof(BuildingPrefab),
	typeof(RoutePrefab),
	typeof(ServiceFeeParameterPrefab)
})]
public class DefaultPolicies : ComponentBase
{
	public DefaultPolicyInfo[] m_Policies;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Policies != null)
		{
			for (int i = 0; i < m_Policies.Length; i++)
			{
				prefabs.Add(m_Policies[i].m_Policy);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<DefaultPolicyData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (!components.Contains(ComponentType.ReadWrite<Waypoint>()) && !components.Contains(ComponentType.ReadWrite<Segment>()))
		{
			components.Add(ComponentType.ReadWrite<Policy>());
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		if (m_Policies != null)
		{
			PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
			DynamicBuffer<DefaultPolicyData> buffer = entityManager.GetBuffer<DefaultPolicyData>(entity);
			for (int i = 0; i < m_Policies.Length; i++)
			{
				DefaultPolicyInfo defaultPolicyInfo = m_Policies[i];
				buffer.Add(new DefaultPolicyData(existingSystemManaged.GetEntity(defaultPolicyInfo.m_Policy)));
			}
		}
	}
}
