using System;
using System.Collections.Generic;
using Colossal.Collections;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class IconConfigurationPrefab : PrefabBase
{
	public Material m_Material;

	public NotificationIconPrefab m_SelectedMarker;

	public NotificationIconPrefab m_FollowedMarker;

	public IconAnimationInfo[] m_Animations;

	public Texture2D m_MissingIcon;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_SelectedMarker);
		prefabs.Add(m_FollowedMarker);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<IconConfigurationData>());
		components.Add(ComponentType.ReadWrite<IconAnimationElement>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem existingSystemManaged = entityManager.World.GetExistingSystemManaged<PrefabSystem>();
		IconConfigurationData componentData = default(IconConfigurationData);
		componentData.m_SelectedMarker = existingSystemManaged.GetEntity(m_SelectedMarker);
		componentData.m_FollowedMarker = existingSystemManaged.GetEntity(m_FollowedMarker);
		entityManager.SetComponentData(entity, componentData);
		if (m_Animations == null)
		{
			return;
		}
		DynamicBuffer<IconAnimationElement> buffer = entityManager.GetBuffer<IconAnimationElement>(entity);
		for (int i = 0; i < m_Animations.Length; i++)
		{
			IconAnimationInfo iconAnimationInfo = m_Animations[i];
			IconAnimationElement iconAnimationElement = new IconAnimationElement
			{
				m_Duration = iconAnimationInfo.m_Duration,
				m_AnimationCurve = new AnimationCurve3(iconAnimationInfo.m_Scale, iconAnimationInfo.m_Alpha, iconAnimationInfo.m_ScreenY)
			};
			int type = (int)iconAnimationInfo.m_Type;
			if (buffer.Length > type)
			{
				buffer[type] = iconAnimationElement;
				continue;
			}
			while (buffer.Length < type)
			{
				buffer.Add(default(IconAnimationElement));
			}
			buffer.Add(iconAnimationElement);
		}
	}
}
