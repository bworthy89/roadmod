using System;
using System.Collections.Generic;
using Game.Common;
using Game.Net;
using Game.Objects;
using Game.Rendering;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Net/Prefab/", new Type[] { })]
public class NetGeometryPrefab : NetPrefab
{
	public NetSectionInfo[] m_Sections;

	public NetEdgeStateInfo[] m_EdgeStates;

	public NetNodeStateInfo[] m_NodeStates;

	public float m_MaxSlopeSteepness = 0.2f;

	public AggregateNetPrefab m_AggregateType;

	public GroupPrefab m_StyleType;

	public CompositionInvertMode m_InvertMode;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		for (int i = 0; i < m_Sections.Length; i++)
		{
			prefabs.Add(m_Sections[i].m_Section);
		}
		if (m_AggregateType != null)
		{
			prefabs.Add(m_AggregateType);
		}
		if (m_StyleType != null)
		{
			prefabs.Add(m_StyleType);
		}
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<NetGeometryData>());
		components.Add(ComponentType.ReadWrite<NetGeometryComposition>());
		components.Add(ComponentType.ReadWrite<NetGeometrySection>());
		components.Add(ComponentType.ReadWrite<NetGeometryEdgeState>());
		components.Add(ComponentType.ReadWrite<NetGeometryNodeState>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		if (components.Contains(ComponentType.ReadWrite<Node>()))
		{
			components.Add(ComponentType.ReadWrite<Game.Net.SubLane>());
			components.Add(ComponentType.ReadWrite<Game.Objects.SubObject>());
			components.Add(ComponentType.ReadWrite<NodeGeometry>());
			components.Add(ComponentType.ReadWrite<CullingInfo>());
			components.Add(ComponentType.ReadWrite<MeshBatch>());
			components.Add(ComponentType.ReadWrite<PseudoRandomSeed>());
		}
		else if (components.Contains(ComponentType.ReadWrite<Edge>()))
		{
			if (m_AggregateType != null)
			{
				components.Add(ComponentType.ReadWrite<Aggregated>());
			}
			components.Add(ComponentType.ReadWrite<Game.Net.SubLane>());
			components.Add(ComponentType.ReadWrite<Game.Objects.SubObject>());
			components.Add(ComponentType.ReadWrite<Curve>());
			components.Add(ComponentType.ReadWrite<Composition>());
			components.Add(ComponentType.ReadWrite<EdgeGeometry>());
			components.Add(ComponentType.ReadWrite<StartNodeGeometry>());
			components.Add(ComponentType.ReadWrite<EndNodeGeometry>());
			components.Add(ComponentType.ReadWrite<BuildOrder>());
			components.Add(ComponentType.ReadWrite<CullingInfo>());
			components.Add(ComponentType.ReadWrite<MeshBatch>());
			components.Add(ComponentType.ReadWrite<PseudoRandomSeed>());
		}
		else if (components.Contains(ComponentType.ReadWrite<NetCompositionData>()))
		{
			components.Add(ComponentType.ReadWrite<NetCompositionPiece>());
			components.Add(ComponentType.ReadWrite<NetCompositionMeshRef>());
			components.Add(ComponentType.ReadWrite<NetCompositionArea>());
			components.Add(ComponentType.ReadWrite<NetCompositionObject>());
			components.Add(ComponentType.ReadWrite<NetCompositionCarriageway>());
		}
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		NetGeometryData componentData = entityManager.GetComponentData<NetGeometryData>(entity);
		List<ComponentBase> list = new List<ComponentBase>();
		GetComponents(list);
		HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
		hashSet.Add(ComponentType.ReadWrite<NetCompositionData>());
		for (int i = 0; i < list.Count; i++)
		{
			list[i].GetArchetypeComponents(hashSet);
		}
		hashSet.Add(ComponentType.ReadWrite<Created>());
		hashSet.Add(ComponentType.ReadWrite<Updated>());
		hashSet.Add(ComponentType.ReadWrite<NetCompositionCrosswalk>());
		componentData.m_NodeCompositionArchetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet));
		hashSet.Remove(ComponentType.ReadWrite<NetCompositionCrosswalk>());
		hashSet.Add(ComponentType.ReadWrite<NetCompositionLane>());
		componentData.m_EdgeCompositionArchetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet));
		entityManager.SetComponentData(entity, componentData);
	}
}
