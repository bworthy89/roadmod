using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Areas/", new Type[] { })]
public class CityBoundaryPrefab : PrefabBase
{
	public Material m_Material;

	public float m_Width = 20f;

	public float m_TilingLength = 80f;

	public Color m_CityBorderColor = new Color(1f, 1f, 1f, 0.75f);

	public Color m_MapBorderColor = new Color(1f, 1f, 1f, 0.25f);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<CityBoundaryData>());
	}
}
