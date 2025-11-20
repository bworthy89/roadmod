using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { typeof(RenderingSettingsPrefab) })]
public class MeshSettings : ComponentBase
{
	public RenderPrefab m_MissingObjectMesh;

	public RenderPrefab m_DefaultBaseMesh;

	public NetSectionPrefab m_MissingNetSection;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_MissingObjectMesh);
		prefabs.Add(m_DefaultBaseMesh);
		prefabs.Add(m_MissingNetSection);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<MeshSettingsData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		MeshSettingsData componentData = new MeshSettingsData
		{
			m_MissingObjectMesh = existingSystemManaged.GetEntity(m_MissingObjectMesh),
			m_DefaultBaseMesh = existingSystemManaged.GetEntity(m_DefaultBaseMesh),
			m_MissingNetSection = existingSystemManaged.GetEntity(m_MissingNetSection)
		};
		entityManager.SetComponentData(entity, componentData);
	}
}
