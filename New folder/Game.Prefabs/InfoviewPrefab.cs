using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Serialization;

namespace Game.Prefabs;

[ComponentMenu("Tools/", new Type[] { })]
public class InfoviewPrefab : PrefabBase
{
	public InfomodeInfo[] m_Infomodes;

	public Color m_DefaultColor = new Color(0.7f, 0.7f, 0.7f, 1f);

	public Color m_SecondaryColor = new Color(0.6f, 0.6f, 0.6f, 1f);

	[FormerlySerializedAs("m_IconName")]
	public string m_IconPath;

	public int m_Priority;

	public int m_Group;

	public IconCategory[] m_WarningCategories;

	public NotificationIconPrefab m_EnableNotificationIcon;

	public bool m_Editor;

	public bool isValid { get; private set; }

	public override bool ignoreUnlockDependencies => true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Infomodes != null)
		{
			for (int i = 0; i < m_Infomodes.Length; i++)
			{
				prefabs.Add(m_Infomodes[i].m_Mode);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<InfoviewData>());
		components.Add(ComponentType.ReadWrite<InfoviewMode>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		InfoviewData componentData = default(InfoviewData);
		componentData.m_NotificationMask = 0u;
		if (m_WarningCategories != null)
		{
			for (int i = 0; i < m_WarningCategories.Length; i++)
			{
				componentData.m_NotificationMask |= (uint)(1 << (int)m_WarningCategories[i]);
			}
		}
		entityManager.SetComponentData(entity, componentData);
		isValid = m_Infomodes != null && m_Infomodes.Length != 0;
	}
}
