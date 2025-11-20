using System;
using System.Collections.Generic;
using Game.Objects;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[] { typeof(BuildingPrefab) })]
public class PlaceholderBuilding : ComponentBase
{
	public const int kStatLevel = 1;

	public BuildingType m_BuildingType;

	public ZonePrefab m_ZoneType;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_ZoneType);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<PlaceholderBuildingData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Placeholder>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		entityManager.SetComponentData(entity, new PlaceholderBuildingData
		{
			m_Type = m_BuildingType,
			m_ZonePrefab = ((m_ZoneType != null) ? existingSystemManaged.GetEntity(m_ZoneType) : Entity.Null)
		});
	}
}
