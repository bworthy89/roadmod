using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

public abstract class UnlockableBase : ComponentBase
{
	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public virtual void LateInitialize(EntityManager entityManager, Entity entity, List<PrefabBase> dependencies)
	{
		LateInitialize(entityManager, entity);
	}

	public static void DefaultLateInitialize(EntityManager entityManager, Entity entity, List<PrefabBase> dependencies)
	{
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		DynamicBuffer<UnlockRequirement> buffer = entityManager.GetBuffer<UnlockRequirement>(entity);
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
}
