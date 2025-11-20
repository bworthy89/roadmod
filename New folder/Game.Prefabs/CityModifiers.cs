using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Policies/", new Type[] { typeof(PolicyPrefab) })]
public class CityModifiers : ComponentBase
{
	public CityModifierInfo[] m_Modifiers;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<CityModifierData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (m_Modifiers != null)
		{
			DynamicBuffer<CityModifierData> buffer = entityManager.GetBuffer<CityModifierData>(entity);
			for (int i = 0; i < m_Modifiers.Length; i++)
			{
				CityModifierInfo cityModifierInfo = m_Modifiers[i];
				buffer.Add(new CityModifierData(cityModifierInfo.m_Type, cityModifierInfo.m_Mode, cityModifierInfo.m_Range));
			}
		}
	}
}
