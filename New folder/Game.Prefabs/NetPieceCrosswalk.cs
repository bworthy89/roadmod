using System;
using System.Collections.Generic;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(NetPiecePrefab) })]
public class NetPieceCrosswalk : ComponentBase
{
	public NetLanePrefab m_Lane;

	public float3 m_Start;

	public float3 m_End;

	public override bool ignoreUnlockDependencies => true;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_Lane);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<NetCrosswalkData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
	}
}
