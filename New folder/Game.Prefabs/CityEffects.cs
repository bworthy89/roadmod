using System;
using System.Collections.Generic;
using Colossal.Mathematics;
using Game.Buildings;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab)
})]
public class CityEffects : ComponentBase, IServiceUpgrade
{
	public CityEffectInfo[] m_Effects;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<CityModifierData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (GetComponent<ServiceUpgrade>() == null)
		{
			components.Add(ComponentType.ReadWrite<CityEffectProvider>());
		}
	}

	public void GetUpgradeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<CityEffectProvider>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (m_Effects != null)
		{
			DynamicBuffer<CityModifierData> buffer = entityManager.GetBuffer<CityModifierData>(entity);
			for (int i = 0; i < m_Effects.Length; i++)
			{
				CityEffectInfo cityEffectInfo = m_Effects[i];
				buffer.Add(new CityModifierData(cityEffectInfo.m_Type, cityEffectInfo.m_Mode, new Bounds1(0f, cityEffectInfo.m_Delta)));
			}
		}
	}
}
