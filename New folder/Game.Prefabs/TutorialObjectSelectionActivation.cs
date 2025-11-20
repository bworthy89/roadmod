using System;
using System.Collections.Generic;
using Colossal.Annotations;
using Game.Tutorials;
using Unity.Collections;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Tutorials/Activation/", new Type[] { typeof(TutorialPrefab) })]
public class TutorialObjectSelectionActivation : TutorialActivation
{
	[NotNull]
	public PrefabBase[] m_Prefabs;

	public bool m_AllowTool = true;

	public override bool ignoreUnlockDependencies => true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		PrefabBase[] prefabs2 = m_Prefabs;
		foreach (PrefabBase item in prefabs2)
		{
			prefabs.Add(item);
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ObjectSelectionActivationData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		DynamicBuffer<ObjectSelectionActivationData> buffer = entityManager.GetBuffer<ObjectSelectionActivationData>(entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		PrefabBase[] prefabs = m_Prefabs;
		foreach (PrefabBase prefabBase in prefabs)
		{
			if (existingSystemManaged.TryGetEntity(prefabBase, out var entity2))
			{
				buffer.Add(new ObjectSelectionActivationData(entity2, m_AllowTool));
			}
		}
	}

	public override void GenerateTutorialLinks(EntityManager entityManager, NativeParallelHashSet<Entity> linkedPrefabs)
	{
		base.GenerateTutorialLinks(entityManager, linkedPrefabs);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		for (int i = 0; i < m_Prefabs.Length; i++)
		{
			linkedPrefabs.Add(existingSystemManaged.GetEntity(m_Prefabs[i]));
		}
	}
}
