using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Prefabs/Unlocking/", new Type[]
{
	typeof(BuildingPrefab),
	typeof(BuildingExtensionPrefab),
	typeof(NetPrefab),
	typeof(StaticObjectPrefab)
})]
public class UnlockOnBuild : ComponentBase
{
	public ObjectBuiltRequirementPrefab[] m_Unlocks;

	public override bool ignoreUnlockDependencies => true;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<UnlockOnBuildData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_Unlocks.Length; i++)
		{
			prefabs.Add(m_Unlocks[i]);
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		DynamicBuffer<UnlockOnBuildData> buffer = entityManager.GetBuffer<UnlockOnBuildData>(entity);
		for (int i = 0; i < m_Unlocks.Length; i++)
		{
			Entity entity2 = existingSystemManaged.GetEntity(m_Unlocks[i]);
			buffer.Add(new UnlockOnBuildData
			{
				m_Entity = entity2
			});
		}
	}
}
