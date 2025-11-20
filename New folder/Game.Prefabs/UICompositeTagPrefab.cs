using System;
using System.Collections.Generic;
using System.Linq;
using Colossal.OdinSerializer.Utilities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("UI/", new Type[] { })]
public class UICompositeTagPrefab : UITagPrefabBase
{
	[Tooltip("This prefab generates a single tag by concatenating the tags of the items in this array, in the order they appear. The tag is treated as a single tag by the Tutorials system.")]
	public PrefabBase[] m_UITagProviders;

	public override string uiTag
	{
		get
		{
			if (!m_Override.IsNullOrWhitespace())
			{
				return m_Override;
			}
			if (m_UITagProviders == null)
			{
				return base.uiTag;
			}
			return string.Join("+", m_UITagProviders.Select((PrefabBase t) => t.uiTag));
		}
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_UITagProviders != null)
		{
			for (int i = 0; i < m_UITagProviders.Length; i++)
			{
				prefabs.Add(m_UITagProviders[i]);
			}
		}
	}
}
