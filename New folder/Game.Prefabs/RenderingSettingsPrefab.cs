using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class RenderingSettingsPrefab : PrefabBase
{
	public Color m_HoveredColor = new Color(0.5f, 0.5f, 1f, 0.1f);

	public Color m_OverrideColor = new Color(1f, 1f, 1f, 0.1f);

	public Color m_WarningColor = new Color(1f, 1f, 0.5f, 0.1f);

	public Color m_ErrorColor = new Color(1f, 0.5f, 0.5f, 0.1f);

	public Color m_OwnerColor = new Color(0.5f, 1f, 0.5f, 0.1f);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<RenderingSettingsData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		RenderingSettingsData componentData = new RenderingSettingsData
		{
			m_HoveredColor = m_HoveredColor,
			m_OverrideColor = m_OverrideColor,
			m_WarningColor = m_WarningColor,
			m_ErrorColor = m_ErrorColor,
			m_OwnerColor = m_OwnerColor
		};
		entityManager.SetComponentData(entity, componentData);
	}
}
