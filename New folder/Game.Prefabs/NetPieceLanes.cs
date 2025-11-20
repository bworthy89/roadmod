using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetPiecePrefab) })]
public class NetPieceLanes : ComponentBase
{
	public NetLaneInfo[] m_Lanes;

	public override bool ignoreUnlockDependencies => true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_Lanes != null)
		{
			for (int i = 0; i < m_Lanes.Length; i++)
			{
				prefabs.Add(m_Lanes[i].m_Lane);
			}
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<NetPieceLane>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
