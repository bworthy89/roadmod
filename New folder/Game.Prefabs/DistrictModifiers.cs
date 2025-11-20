using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Policies/", new Type[] { typeof(PolicyPrefab) })]
public class DistrictModifiers : ComponentBase
{
	public DistrictModifierInfo[] m_Modifiers;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<DistrictModifierData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (m_Modifiers != null)
		{
			DynamicBuffer<DistrictModifierData> buffer = entityManager.GetBuffer<DistrictModifierData>(entity);
			for (int i = 0; i < m_Modifiers.Length; i++)
			{
				DistrictModifierInfo districtModifierInfo = m_Modifiers[i];
				buffer.Add(new DistrictModifierData(districtModifierInfo.m_Type, districtModifierInfo.m_Mode, districtModifierInfo.m_Range));
			}
		}
	}
}
