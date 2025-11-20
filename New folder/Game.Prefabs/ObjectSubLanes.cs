using System;
using System.Collections.Generic;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[] { typeof(StaticObjectPrefab) })]
public class ObjectSubLanes : ComponentBase
{
	public ObjectSubLaneInfo[] m_SubLanes;

	public override bool ignoreUnlockDependencies => true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_SubLanes != null)
		{
			for (int i = 0; i < m_SubLanes.Length; i++)
			{
				prefabs.Add(m_SubLanes[i].m_LanePrefab);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<SubLane>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		bool flag = false;
		if (m_SubLanes != null)
		{
			for (int i = 0; i < m_SubLanes.Length; i++)
			{
				ObjectSubLaneInfo objectSubLaneInfo = m_SubLanes[i];
				if (objectSubLaneInfo.m_NodeIndex.x != objectSubLaneInfo.m_NodeIndex.y)
				{
					flag = true;
					break;
				}
			}
		}
		if (flag)
		{
			components.Add(ComponentType.ReadWrite<Game.Net.SubLane>());
		}
	}
}
