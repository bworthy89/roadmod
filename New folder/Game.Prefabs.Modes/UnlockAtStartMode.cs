using System;
using System.Collections.Generic;
using Colossal.Entities;
using Game.Common;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Component/", new Type[] { })]
public class UnlockAtStartMode : LocalModePrefab
{
	public PrefabBase[] m_Prefabs;

	public PrefabBase m_Requirement;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_Prefabs.Length; i++)
		{
			PrefabBase prefabBase = m_Prefabs[i];
			prefabSystem.GetEntity(prefabBase);
		}
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		Entity entity = Entity.Null;
		if (m_Requirement != null)
		{
			entity = prefabSystem.GetEntity(m_Requirement);
		}
		for (int i = 0; i < m_Prefabs.Length; i++)
		{
			PrefabBase prefabBase = m_Prefabs[i];
			Entity entity2 = prefabSystem.GetEntity(prefabBase);
			if (entityManager.TryGetBuffer(entity2, isReadOnly: false, out DynamicBuffer<UnlockRequirement> buffer))
			{
				buffer.Clear();
				if (entity != Entity.Null)
				{
					buffer.Add(new UnlockRequirement(entity, UnlockFlags.RequireAll));
				}
				entityManager.AddComponentData(entity2, default(Updated));
			}
		}
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		for (int i = 0; i < m_Prefabs.Length; i++)
		{
			PrefabBase prefabBase = m_Prefabs[i];
			if (prefabBase.TryGetExactly<Unlockable>(out var component))
			{
				Entity entity = prefabSystem.GetEntity(prefabBase);
				List<PrefabBase> list = new List<PrefabBase>();
				prefabBase.GetDependencies(list);
				component.LateInitialize(entityManager, entity, list);
			}
		}
	}
}
