using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Policies/", new Type[] { typeof(PolicyPrefab) })]
public class BuildingModifiers : ComponentBase
{
	public BuildingModifierInfo[] m_Modifiers;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<BuildingModifierData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (m_Modifiers != null)
		{
			DynamicBuffer<BuildingModifierData> buffer = entityManager.GetBuffer<BuildingModifierData>(entity);
			for (int i = 0; i < m_Modifiers.Length; i++)
			{
				BuildingModifierInfo buildingModifierInfo = m_Modifiers[i];
				buffer.Add(new BuildingModifierData(buildingModifierInfo.m_Type, buildingModifierInfo.m_Mode, buildingModifierInfo.m_Range));
			}
		}
	}
}
