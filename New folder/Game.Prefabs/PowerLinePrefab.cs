using System;
using System.Collections.Generic;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/Prefab/", new Type[] { })]
public class PowerLinePrefab : NetGeometryPrefab
{
	public float m_MaxPylonDistance = 120f;

	public float m_Hanging;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<PowerLineData>());
		components.Add(ComponentType.ReadWrite<LocalConnectData>());
		components.Add(ComponentType.ReadWrite<DefaultNetLane>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		if (components.Contains(ComponentType.ReadWrite<Edge>()))
		{
			components.Add(ComponentType.ReadWrite<EdgeColor>());
		}
		else if (components.Contains(ComponentType.ReadWrite<Node>()))
		{
			components.Add(ComponentType.ReadWrite<NodeColor>());
		}
	}
}
