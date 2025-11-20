using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Diversity/", new Type[] { })]
public class AtmospherePrefab : PrefabBase
{
	public Texture2D m_MoonAlbedo;

	public Texture2D m_MoonNormal;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<AtmospherePrefabData>());
	}
}
