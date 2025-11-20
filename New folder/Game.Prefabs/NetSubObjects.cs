using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetPrefab) })]
public class NetSubObjects : ComponentBase
{
	public NetSubObjectInfo[] m_SubObjects;

	public override bool ignoreUnlockDependencies => true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_SubObjects.Length; i++)
		{
			prefabs.Add(m_SubObjects[i].m_Object);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<SubObject>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
