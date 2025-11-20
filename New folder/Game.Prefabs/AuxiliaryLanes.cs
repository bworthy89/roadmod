using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetLanePrefab) })]
public class AuxiliaryLanes : ComponentBase
{
	public AuxiliaryLaneInfo[] m_AuxiliaryLanes;

	public override bool ignoreUnlockDependencies => true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_AuxiliaryLanes.Length; i++)
		{
			prefabs.Add(m_AuxiliaryLanes[i].m_Lane);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<AuxiliaryNetLane>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
