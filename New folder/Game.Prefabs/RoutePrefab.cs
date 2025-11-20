using System;
using System.Collections.Generic;
using Game.Common;
using Game.Rendering;
using Game.Routes;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Routes/", new Type[] { })]
public class RoutePrefab : PrefabBase, IColored
{
	public Material m_Material;

	public float m_Width = 4f;

	public float m_SegmentLength = 64f;

	public UnityEngine.Color m_Color = UnityEngine.Color.magenta;

	public string m_LocaleID;

	public Color32 color => m_Color;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<RouteData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		if (components.Contains(ComponentType.ReadWrite<Route>()))
		{
			components.Add(ComponentType.ReadWrite<RouteWaypoint>());
			components.Add(ComponentType.ReadWrite<RouteSegment>());
			components.Add(ComponentType.ReadWrite<Game.Routes.Color>());
			components.Add(ComponentType.ReadWrite<RouteBufferIndex>());
		}
		else if (components.Contains(ComponentType.ReadWrite<Waypoint>()))
		{
			components.Add(ComponentType.ReadWrite<Position>());
			components.Add(ComponentType.ReadWrite<Owner>());
		}
		else if (components.Contains(ComponentType.ReadWrite<Segment>()))
		{
			components.Add(ComponentType.ReadWrite<CurveElement>());
			components.Add(ComponentType.ReadWrite<Owner>());
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		List<ComponentBase> list = new List<ComponentBase>();
		GetComponents(list);
		HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
		HashSet<ComponentType> hashSet2 = new HashSet<ComponentType>();
		HashSet<ComponentType> hashSet3 = new HashSet<ComponentType>();
		HashSet<ComponentType> hashSet4 = new HashSet<ComponentType>();
		hashSet.Add(ComponentType.ReadWrite<Route>());
		hashSet2.Add(ComponentType.ReadWrite<Waypoint>());
		hashSet3.Add(ComponentType.ReadWrite<Waypoint>());
		hashSet3.Add(ComponentType.ReadWrite<Connected>());
		hashSet4.Add(ComponentType.ReadWrite<Segment>());
		for (int i = 0; i < list.Count; i++)
		{
			list[i].GetArchetypeComponents(hashSet);
			list[i].GetArchetypeComponents(hashSet2);
			list[i].GetArchetypeComponents(hashSet3);
			list[i].GetArchetypeComponents(hashSet4);
		}
		hashSet.Add(ComponentType.ReadWrite<Created>());
		hashSet2.Add(ComponentType.ReadWrite<Created>());
		hashSet3.Add(ComponentType.ReadWrite<Created>());
		hashSet4.Add(ComponentType.ReadWrite<Created>());
		hashSet.Add(ComponentType.ReadWrite<Updated>());
		hashSet2.Add(ComponentType.ReadWrite<Updated>());
		hashSet3.Add(ComponentType.ReadWrite<Updated>());
		hashSet4.Add(ComponentType.ReadWrite<Updated>());
		RouteData componentData = entityManager.GetComponentData<RouteData>(entity);
		componentData.m_RouteArchetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet));
		componentData.m_WaypointArchetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet2));
		componentData.m_ConnectedArchetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet3));
		componentData.m_SegmentArchetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet4));
		entityManager.SetComponentData(entity, componentData);
	}
}
