using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Prefabs/Unlocking/", new Type[] { })]
public class Unlockable : UnlockableBase
{
	public PrefabBase[] m_RequireAll;

	public PrefabBase[] m_RequireAny;

	public bool m_IgnoreDependencies;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_RequireAll != null)
		{
			for (int i = 0; i < m_RequireAll.Length; i++)
			{
				prefabs.Add(m_RequireAll[i]);
			}
		}
		if (m_RequireAny != null)
		{
			for (int j = 0; j < m_RequireAny.Length; j++)
			{
				prefabs.Add(m_RequireAny[j]);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity, List<PrefabBase> dependencies)
	{
		base.LateInitialize(entityManager, entity, dependencies);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		DynamicBuffer<UnlockRequirement> buffer = entityManager.GetBuffer<UnlockRequirement>(entity);
		if (!m_IgnoreDependencies)
		{
			for (int i = 0; i < dependencies.Count; i++)
			{
				PrefabBase prefabBase = dependencies[i];
				if (existingSystemManaged.IsUnlockable(prefabBase))
				{
					Entity entity2 = existingSystemManaged.GetEntity(prefabBase);
					buffer.Add(new UnlockRequirement(entity2, UnlockFlags.RequireAll));
				}
			}
		}
		if (m_RequireAll != null)
		{
			for (int j = 0; j < m_RequireAll.Length; j++)
			{
				PrefabBase prefabBase2 = m_RequireAll[j];
				if (existingSystemManaged.IsUnlockable(prefabBase2))
				{
					Entity entity3 = existingSystemManaged.GetEntity(prefabBase2);
					buffer.Add(new UnlockRequirement(entity3, UnlockFlags.RequireAll));
				}
			}
		}
		if (m_RequireAny == null)
		{
			return;
		}
		for (int k = 0; k < m_RequireAny.Length; k++)
		{
			PrefabBase prefabBase3 = m_RequireAny[k];
			if (existingSystemManaged.IsUnlockable(prefabBase3))
			{
				Entity entity4 = existingSystemManaged.GetEntity(prefabBase3);
				buffer.Add(new UnlockRequirement(entity4, UnlockFlags.RequireAny));
			}
		}
	}
}
