using System;
using System.Collections.Generic;
using Game.Common;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/Lane/", new Type[] { })]
public class NetLanePrefab : PrefabBase
{
	public PathfindPrefab m_PathfindPrefab;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		if (m_PathfindPrefab != null)
		{
			prefabs.Add(m_PathfindPrefab);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<NetLaneData>());
		components.Add(ComponentType.ReadWrite<NetLaneArchetypeData>());
		if (!base.prefab.Has<SecondaryLane>())
		{
			components.Add(ComponentType.ReadWrite<SecondaryNetLane>());
		}
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<Curve>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		List<ComponentBase> list = new List<ComponentBase>();
		GetComponents(list);
		HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
		hashSet.Add(ComponentType.ReadWrite<Lane>());
		NetLaneArchetypeData componentData = default(NetLaneArchetypeData);
		componentData.m_LaneArchetype = CreateArchetype(entityManager, list, hashSet);
		hashSet.Add(ComponentType.ReadWrite<Lane>());
		hashSet.Add(ComponentType.ReadWrite<AreaLane>());
		componentData.m_AreaLaneArchetype = CreateArchetype(entityManager, list, hashSet);
		hashSet.Add(ComponentType.ReadWrite<Lane>());
		hashSet.Add(ComponentType.ReadWrite<EdgeLane>());
		componentData.m_EdgeLaneArchetype = CreateArchetype(entityManager, list, hashSet);
		hashSet.Add(ComponentType.ReadWrite<Lane>());
		hashSet.Add(ComponentType.ReadWrite<NodeLane>());
		componentData.m_NodeLaneArchetype = CreateArchetype(entityManager, list, hashSet);
		hashSet.Add(ComponentType.ReadWrite<Lane>());
		hashSet.Add(ComponentType.ReadWrite<SlaveLane>());
		hashSet.Add(ComponentType.ReadWrite<EdgeLane>());
		componentData.m_EdgeSlaveArchetype = CreateArchetype(entityManager, list, hashSet);
		hashSet.Add(ComponentType.ReadWrite<Lane>());
		hashSet.Add(ComponentType.ReadWrite<SlaveLane>());
		hashSet.Add(ComponentType.ReadWrite<NodeLane>());
		componentData.m_NodeSlaveArchetype = CreateArchetype(entityManager, list, hashSet);
		hashSet.Add(ComponentType.ReadWrite<Lane>());
		hashSet.Add(ComponentType.ReadWrite<MasterLane>());
		hashSet.Add(ComponentType.ReadWrite<EdgeLane>());
		componentData.m_EdgeMasterArchetype = CreateArchetype(entityManager, list, hashSet);
		hashSet.Add(ComponentType.ReadWrite<Lane>());
		hashSet.Add(ComponentType.ReadWrite<MasterLane>());
		hashSet.Add(ComponentType.ReadWrite<NodeLane>());
		componentData.m_NodeMasterArchetype = CreateArchetype(entityManager, list, hashSet);
		entityManager.SetComponentData(entity, componentData);
	}

	private EntityArchetype CreateArchetype(EntityManager entityManager, List<ComponentBase> unityComponents, HashSet<ComponentType> laneComponents)
	{
		for (int i = 0; i < unityComponents.Count; i++)
		{
			unityComponents[i].GetArchetypeComponents(laneComponents);
		}
		laneComponents.Add(ComponentType.ReadWrite<Created>());
		laneComponents.Add(ComponentType.ReadWrite<Updated>());
		EntityArchetype result = entityManager.CreateArchetype(PrefabUtils.ToArray(laneComponents));
		laneComponents.Clear();
		return result;
	}
}
