using System;
using System.Collections.Generic;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[]
{
	typeof(RoadPrefab),
	typeof(TrackPrefab),
	typeof(PathwayPrefab)
})]
public class Bridge : ComponentBase
{
	public float m_SegmentLength = 100f;

	public float m_Hanging;

	public float m_ElevationOnWater = 10f;

	public bool m_CanCurve;

	public bool m_AllowMinimalLength;

	public BridgeWaterFlow m_WaterFlow;

	public BridgeBuildStyle m_BuildStyle;

	public FixedNetSegmentInfo[] m_FixedSegments;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<BridgeData>());
		if (m_FixedSegments != null && m_FixedSegments.Length != 0)
		{
			components.Add(ComponentType.ReadWrite<FixedNetElement>());
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		if (m_FixedSegments != null && m_FixedSegments.Length != 0 && components.Contains(ComponentType.ReadWrite<Edge>()))
		{
			components.Add(ComponentType.ReadWrite<Fixed>());
		}
	}
}
