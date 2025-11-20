using System;
using System.Collections.Generic;
using Game.Zones;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Zones/", new Type[] { })]
public class ZonePrefab : PrefabBase
{
	public AreaType m_AreaType;

	public Color m_Color = Color.white;

	public Color m_Edge = Color.white;

	public bool m_Office;

	public override IEnumerable<string> modTags
	{
		get
		{
			foreach (string modTag in base.modTags)
			{
				yield return modTag;
			}
			yield return "Zones";
			if (m_Office)
			{
				yield return "ZonesOffice";
			}
			else
			{
				yield return $"Zones{m_AreaType}";
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ZoneData>());
		components.Add(ComponentType.ReadWrite<PlaceableInfoviewItem>());
		components.Add(ComponentType.ReadWrite<ProcessEstimate>());
	}

	public void GetBuildingPrefabComponents(HashSet<ComponentType> components, BuildingPrefab buildingPrefab, byte level)
	{
		if (m_Office)
		{
			components.Add(ComponentType.ReadWrite<OfficeBuilding>());
		}
		List<IZoneBuildingComponent> list = new List<IZoneBuildingComponent>();
		if (!base.prefab.TryGet(list))
		{
			return;
		}
		foreach (IZoneBuildingComponent item in list)
		{
			item.GetBuildingPrefabComponents(components, buildingPrefab, level);
		}
	}

	public void GetBuildingArchetypeComponents(HashSet<ComponentType> components, BuildingPrefab buildingPrefab, byte level)
	{
		List<IZoneBuildingComponent> list = new List<IZoneBuildingComponent>();
		if (!base.prefab.TryGet(list))
		{
			return;
		}
		foreach (IZoneBuildingComponent item in list)
		{
			item.GetBuildingArchetypeComponents(components, buildingPrefab, level);
		}
	}

	public void InitializeBuilding(EntityManager entityManager, Entity entity, BuildingPrefab buildingPrefab, byte level)
	{
		List<IZoneBuildingComponent> list = new List<IZoneBuildingComponent>();
		if (!base.prefab.TryGet(list))
		{
			return;
		}
		foreach (IZoneBuildingComponent item in list)
		{
			item.InitializeBuilding(entityManager, entity, buildingPrefab, level);
		}
	}
}
