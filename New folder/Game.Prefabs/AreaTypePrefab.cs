using System;
using System.Collections.Generic;
using Game.Areas;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Areas/", new Type[] { })]
public class AreaTypePrefab : PrefabBase
{
	public AreaType m_Type;

	public Material m_Material;

	public Material m_NameMaterial;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<AreaTypeData>());
	}
}
