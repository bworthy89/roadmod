using System;
using System.Collections.Generic;
using Game.Zones;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Prefabs/Unlocking/", new Type[] { })]
public class ZoneBuiltRequirementPrefab : UnlockRequirementPrefab
{
	public ThemePrefab m_RequiredTheme;

	public ZonePrefab m_RequiredZone;

	public AreaType m_RequiredType;

	public int m_MinimumSquares = 2500;

	public int m_MinimumCount;

	public int m_MinimumLevel = 1;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_RequiredTheme != null)
		{
			prefabs.Add(m_RequiredTheme);
		}
		if (m_RequiredZone != null)
		{
			prefabs.Add(m_RequiredZone);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ZoneBuiltRequirementData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		entityManager.GetBuffer<UnlockRequirement>(entity).Add(new UnlockRequirement(entity, UnlockFlags.RequireAll));
		ZoneBuiltRequirementData componentData = default(ZoneBuiltRequirementData);
		if (m_RequiredTheme != null)
		{
			componentData.m_RequiredTheme = existingSystemManaged.GetEntity(m_RequiredTheme);
		}
		if (m_RequiredZone != null)
		{
			componentData.m_RequiredZone = existingSystemManaged.GetEntity(m_RequiredZone);
		}
		componentData.m_RequiredType = m_RequiredType;
		componentData.m_MinimumSquares = m_MinimumSquares;
		componentData.m_MinimumCount = m_MinimumCount;
		componentData.m_MinimumLevel = (byte)m_MinimumLevel;
		entityManager.SetComponentData(entity, componentData);
	}
}
