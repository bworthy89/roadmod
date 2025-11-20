using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[] { typeof(BuildingPrefab) })]
public class BuildingModules : ComponentBase
{
	public PrefabBase[] m_Modules;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Modules != null)
		{
			for (int i = 0; i < m_Modules.Length; i++)
			{
				prefabs.Add(m_Modules[i]);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<BuildingModule>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		if (m_Modules == null)
		{
			return;
		}
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		for (int i = 0; i < m_Modules.Length; i++)
		{
			PrefabBase prefabBase = m_Modules[i];
			if (!(prefabBase == null))
			{
				Entity entity2 = existingSystemManaged.GetEntity(prefabBase);
				entityManager.AddComponent<BuildingModuleData>(entity2);
				entityManager.GetBuffer<BuildingModule>(entity).Add(new BuildingModule(entity2));
			}
		}
	}
}
