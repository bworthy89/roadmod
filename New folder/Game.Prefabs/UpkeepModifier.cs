using System;
using System.Collections.Generic;
using Game.Economy;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[] { typeof(BuildingExtensionPrefab) })]
public class UpkeepModifier : ComponentBase, IServiceUpgrade
{
	public UpkeepModifierInfo[] m_Modifiers;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<UpkeepModifierData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<UpkeepModifier>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (m_Modifiers != null)
		{
			DynamicBuffer<UpkeepModifierData> buffer = entityManager.GetBuffer<UpkeepModifierData>(entity);
			for (int i = 0; i < m_Modifiers.Length; i++)
			{
				UpkeepModifierInfo upkeepModifierInfo = m_Modifiers[i];
				buffer.Add(new UpkeepModifierData
				{
					m_Resource = EconomyUtils.GetResource(upkeepModifierInfo.m_Resource),
					m_Multiplier = upkeepModifierInfo.m_Multiplier
				});
			}
		}
	}
}
