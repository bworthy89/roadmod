using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Tools/", new Type[] { typeof(TerraformingPrefab) })]
public class TerrainMaterialProperties : ComponentBase
{
	public Texture m_Texture;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
