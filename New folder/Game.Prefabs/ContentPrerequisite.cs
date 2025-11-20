using System;
using System.Collections.Generic;
using Game.UI.Editor;
using Unity.Entities;

namespace Game.Prefabs;

[HideInEditor]
[ComponentMenu("Prefabs/Content/", new Type[] { })]
public class ContentPrerequisite : ComponentBase
{
	public ContentPrefab m_ContentPrerequisite;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_ContentPrerequisite);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<ContentPrerequisiteData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		if (m_ContentPrerequisite != null)
		{
			PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
			entityManager.SetComponentData(entity, new ContentPrerequisiteData
			{
				m_ContentPrerequisite = existingSystemManaged.GetEntity(m_ContentPrerequisite)
			});
		}
	}
}
