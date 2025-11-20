using System;
using System.Collections.Generic;
using Game.Objects;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[] { typeof(ObjectPrefab) })]
public class PlaceholderObject : ComponentBase
{
	public bool m_RandomizeGroupIndex;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PlaceholderObjectElement>());
		components.Add(ComponentType.ReadWrite<PlaceholderObjectData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Placeholder>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		if (base.prefab.Has<SpawnableObject>())
		{
			ComponentBase.baseLog.WarnFormat(base.prefab, "PlaceholderObject is SpawnableObject: {0}", base.prefab.name);
		}
		PlaceholderObjectData componentData = new PlaceholderObjectData
		{
			m_RandomizeGroupIndex = m_RandomizeGroupIndex
		};
		entityManager.SetComponentData(entity, componentData);
	}
}
