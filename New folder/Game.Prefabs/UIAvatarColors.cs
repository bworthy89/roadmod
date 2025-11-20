using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("UI/", new Type[] { })]
public class UIAvatarColors : PrefabBase
{
	public Color32[] m_AvatarColors;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<UIAvatarColorData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		DynamicBuffer<UIAvatarColorData> buffer = entityManager.GetBuffer<UIAvatarColorData>(entity);
		for (int i = 0; i < m_AvatarColors.Length; i++)
		{
			buffer.Add(new UIAvatarColorData(m_AvatarColors[i]));
		}
		base.LateInitialize(entityManager, entity);
	}
}
