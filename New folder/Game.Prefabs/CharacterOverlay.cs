using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Game.Prefabs;

public class CharacterOverlay : PrefabBase
{
	public int m_Index;

	public int m_SortOrder;

	public Rect m_sourceRegion;

	public Rect m_targetRegion;

	public float4 GetRegionAsFloat4(Rect region)
	{
		return new float4(region.xMin, region.yMin, region.xMax, region.yMax);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<CharacterOverlayData>());
	}
}
