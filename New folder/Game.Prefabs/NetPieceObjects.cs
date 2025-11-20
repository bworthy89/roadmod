using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetPiecePrefab) })]
public class NetPieceObjects : ComponentBase
{
	public NetPieceObjectInfo[] m_PieceObjects;

	public override bool ignoreUnlockDependencies => true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_PieceObjects.Length; i++)
		{
			prefabs.Add(m_PieceObjects[i].m_Object);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<NetPieceObject>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
