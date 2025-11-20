using System;
using System.Collections.Generic;
using Game.Economy;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class InitialResources : ComponentBase, IServiceUpgrade
{
	public InitialResourceItem[] m_InitialResources;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<InitialResourceData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (GetComponent<ServiceUpgrade>() == null)
		{
			components.Add(ComponentType.ReadWrite<Resources>());
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Resources>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		DynamicBuffer<InitialResourceData> dynamicBuffer = entityManager.AddBuffer<InitialResourceData>(entity);
		if (m_InitialResources != null)
		{
			for (int i = 0; i < m_InitialResources.Length; i++)
			{
				dynamicBuffer.Add(new InitialResourceData
				{
					m_Value = new ResourceStack
					{
						m_Resource = EconomyUtils.GetResource(m_InitialResources[i].m_Value.m_Resource),
						m_Amount = m_InitialResources[i].m_Value.m_Amount
					}
				});
			}
		}
	}
}
