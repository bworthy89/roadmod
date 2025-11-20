using System;
using System.Collections.Generic;
using Game.Buildings;
using Game.Objects;
using Game.UI.Editor;
using Game.UI.Widgets;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Buildings/", new Type[] { typeof(BuildingPrefab) })]
public class SignatureBuilding : ComponentBase
{
	public const int kStatLevel = 5;

	public ZonePrefab m_ZoneType;

	public int m_XPReward = 300;

	[CustomField(typeof(UIIconField))]
	public string m_UnlockEventImage;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_ZoneType);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<SignatureBuildingData>());
		components.Add(ComponentType.ReadWrite<SpawnableBuildingData>());
		components.Add(ComponentType.ReadWrite<PlaceableObjectData>());
		components.Add(ComponentType.ReadWrite<PlaceableInfoviewItem>());
		if (m_ZoneType != null)
		{
			m_ZoneType.GetBuildingPrefabComponents(components, (BuildingPrefab)base.prefab, 5);
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<BuildingCondition>());
		components.Add(ComponentType.ReadWrite<Signature>());
		components.Add(ComponentType.ReadWrite<Game.Objects.UniqueObject>());
		if (m_ZoneType != null)
		{
			m_ZoneType.GetBuildingArchetypeComponents(components, (BuildingPrefab)base.prefab, 5);
		}
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		PlaceableObjectData componentData = entityManager.GetComponentData<PlaceableObjectData>(entity);
		componentData.m_XPReward = m_XPReward;
		if ((componentData.m_Flags & (PlacementFlags.Shoreline | PlacementFlags.Floating | PlacementFlags.Hovering)) == 0)
		{
			componentData.m_Flags |= PlacementFlags.OnGround;
		}
		componentData.m_Flags |= PlacementFlags.Unique;
		entityManager.SetComponentData(entity, componentData);
		if (m_ZoneType != null)
		{
			m_ZoneType.InitializeBuilding(entityManager, entity, (BuildingPrefab)base.prefab, 5);
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		SpawnableBuildingData componentData = new SpawnableBuildingData
		{
			m_Level = 5
		};
		if (m_ZoneType != null)
		{
			componentData.m_ZonePrefab = existingSystemManaged.GetEntity(m_ZoneType);
		}
		entityManager.SetComponentData(entity, componentData);
	}
}
