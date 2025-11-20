using System;
using System.Collections.Generic;
using Game.Common;
using Game.Simulation;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class HappinessFactorParameterPrefab : PrefabBase
{
	[EnumValue(typeof(CitizenHappinessSystem.HappinessFactor))]
	public int[] m_BaseLevels = new int[26];

	[EnumValue(typeof(CitizenHappinessSystem.HappinessFactor))]
	public PrefabBase[] m_LockedEntities = new PrefabBase[26];

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_LockedEntities.Length; i++)
		{
			if (m_LockedEntities[i] != null)
			{
				prefabs.Add(m_LockedEntities[i]);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<HappinessFactorParameterData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = World.DefaultGameObjectInjectionWorld.GetOrCreateSystemManaged<PrefabSystem>();
		DynamicBuffer<HappinessFactorParameterData> buffer = entityManager.GetBuffer<HappinessFactorParameterData>(entity);
		for (int i = 0; i < m_BaseLevels.Length; i++)
		{
			Entity lockedEntity = ((m_LockedEntities[i] != null) ? orCreateSystemManaged.GetEntity(m_LockedEntities[i]) : Entity.Null);
			buffer.Add(new HappinessFactorParameterData
			{
				m_BaseLevel = m_BaseLevels[i],
				m_LockedEntity = lockedEntity
			});
		}
	}
}
