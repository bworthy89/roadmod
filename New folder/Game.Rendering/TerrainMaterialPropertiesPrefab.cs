using System.Collections.Generic;
using Game.Prefabs;
using Unity.Entities;
using UnityEngine;

namespace Game.Rendering;

public class TerrainMaterialPropertiesPrefab : PrefabBase
{
	public Material m_SplatmapMaterial;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TerrainMaterialPropertiesData>());
	}
}
