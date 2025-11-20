using System;
using System.Collections.Generic;
using Colossal;
using Game.Areas;
using Game.Rendering;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Areas/", new Type[]
{
	typeof(SurfacePrefab),
	typeof(LotPrefab)
})]
public class RenderedArea : ComponentBase
{
	public Material m_Material;

	public float m_Roundness = 0.5f;

	public float m_LodBias;

	public int m_RendererPriority;

	[BitMask]
	public DecalLayers m_DecalLayerMask = DecalLayers.Terrain;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<RenderedAreaData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Batch>());
	}
}
