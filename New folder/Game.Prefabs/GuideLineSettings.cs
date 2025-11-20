using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { typeof(RenderingSettingsPrefab) })]
public class GuideLineSettings : ComponentBase
{
	[Serializable]
	public class WaterSourceColor
	{
		public Color m_Outline;

		public Color m_Fill;

		public Color m_ProjectedOutline;

		public Color m_ProjectedFill;
	}

	public Color m_VeryLowPriorityColor = new Color(0.7f, 0.7f, 1f, 0.025f);

	public Color m_LowPriorityColor = new Color(0.7f, 0.7f, 1f, 0.05f);

	public Color m_MediumPriorityColor = new Color(0.7f, 0.7f, 1f, 0.1f);

	public Color m_HighPriorityColor = new Color(0.7f, 0.7f, 1f, 0.2f);

	public Color m_PositiveFeedbackColor = new Color(0.5f, 1f, 0.5f, 0.1f);

	public WaterSourceColor[] m_WaterSourceColors;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<GuideLineSettingsData>());
		components.Add(ComponentType.ReadWrite<WaterSourceColorElement>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		GuideLineSettingsData componentData = new GuideLineSettingsData
		{
			m_VeryLowPriorityColor = m_VeryLowPriorityColor,
			m_LowPriorityColor = m_LowPriorityColor,
			m_MediumPriorityColor = m_MediumPriorityColor,
			m_HighPriorityColor = m_HighPriorityColor,
			m_PositiveFeedbackColor = m_PositiveFeedbackColor
		};
		entityManager.SetComponentData(entity, componentData);
		if (m_WaterSourceColors != null)
		{
			DynamicBuffer<WaterSourceColorElement> buffer = entityManager.GetBuffer<WaterSourceColorElement>(entity);
			buffer.ResizeUninitialized(m_WaterSourceColors.Length);
			for (int i = 0; i < m_WaterSourceColors.Length; i++)
			{
				WaterSourceColor waterSourceColor = m_WaterSourceColors[i];
				buffer[i] = new WaterSourceColorElement
				{
					m_Outline = waterSourceColor.m_Outline,
					m_Fill = waterSourceColor.m_Fill,
					m_ProjectedOutline = waterSourceColor.m_ProjectedOutline,
					m_ProjectedFill = waterSourceColor.m_ProjectedFill
				};
			}
		}
	}
}
