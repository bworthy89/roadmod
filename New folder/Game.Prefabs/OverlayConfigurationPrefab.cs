using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class OverlayConfigurationPrefab : PrefabBase
{
	public Material m_CurveMaterial;

	public Material m_SolidObjectMaterial;

	public Material m_TextMaterial;

	public Material m_ObjectBrushMaterial;

	public FontInfo[] m_FontInfos;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<OverlayConfigurationData>());
	}
}
