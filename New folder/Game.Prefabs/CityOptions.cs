using System;
using System.Collections.Generic;
using Game.City;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Policies/", new Type[] { typeof(PolicyPrefab) })]
public class CityOptions : ComponentBase
{
	public CityOption[] m_Options;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<CityOptionData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (m_Options != null)
		{
			CityOptionData componentData = default(CityOptionData);
			for (int i = 0; i < m_Options.Length; i++)
			{
				componentData.m_OptionMask |= (uint)(1 << (int)m_Options[i]);
			}
			entityManager.SetComponentData(entity, componentData);
		}
	}
}
