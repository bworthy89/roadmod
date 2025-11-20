using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Buildings;
using Game.Net;
using Game.Objects;
using Game.Simulation;
using Game.Zones;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/Prefab/", new Type[] { })]
public class RoadPrefab : NetGeometryPrefab
{
	public RoadType m_RoadType;

	public float m_SpeedLimit = 100f;

	public ZoneBlockPrefab m_ZoneBlock;

	public bool m_TrafficLights;

	public bool m_HighwayRules;

	public override IEnumerable<string> modTags
	{
		get
		{
			foreach (string modTag in base.modTags)
			{
				yield return modTag;
			}
			yield return "Roads";
		}
	}

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_ZoneBlock != null)
		{
			prefabs.Add(m_ZoneBlock);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<RoadData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		if (components.Contains(ComponentType.ReadWrite<Edge>()))
		{
			components.Add(ComponentType.ReadWrite<Road>());
			components.Add(ComponentType.ReadWrite<UpdateFrame>());
			components.Add(ComponentType.ReadWrite<LandValue>());
			components.Add(ComponentType.ReadWrite<EdgeColor>());
			components.Add(ComponentType.ReadWrite<NetCondition>());
			components.Add(ComponentType.ReadWrite<MaintenanceConsumer>());
			components.Add(ComponentType.ReadWrite<BorderDistrict>());
			if (m_ZoneBlock != null)
			{
				components.Add(ComponentType.ReadWrite<SubBlock>());
				components.Add(ComponentType.ReadWrite<ConnectedBuilding>());
				components.Add(ComponentType.ReadWrite<Game.Net.ServiceCoverage>());
				components.Add(ComponentType.ReadWrite<ResourceAvailability>());
				components.Add(ComponentType.ReadWrite<Density>());
			}
			else if (!m_HighwayRules)
			{
				components.Add(ComponentType.ReadWrite<ConnectedBuilding>());
			}
		}
		else if (components.Contains(ComponentType.ReadWrite<Game.Net.Node>()))
		{
			components.Add(ComponentType.ReadWrite<Road>());
			components.Add(ComponentType.ReadWrite<UpdateFrame>());
			components.Add(ComponentType.ReadWrite<LandValue>());
			components.Add(ComponentType.ReadWrite<NodeColor>());
			components.Add(ComponentType.ReadWrite<NetCondition>());
			components.Add(ComponentType.ReadWrite<Game.Objects.Surface>());
		}
		else if (components.Contains(ComponentType.ReadWrite<NetCompositionData>()))
		{
			components.Add(ComponentType.ReadWrite<RoadComposition>());
		}
	}
}
