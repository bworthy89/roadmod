using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("UI/", new Type[] { })]
public class EditorAssetCategoryOverride : ComponentBase
{
	public string[] m_IncludeCategories;

	public string[] m_ExcludeCategories;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		if ((m_IncludeCategories != null && m_IncludeCategories.Length != 0) || (m_ExcludeCategories != null && m_ExcludeCategories.Length != 0))
		{
			components.Add(ComponentType.ReadWrite<EditorAssetCategoryOverrideData>());
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
