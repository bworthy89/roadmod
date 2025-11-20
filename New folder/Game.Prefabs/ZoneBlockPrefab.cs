using System;
using System.Collections.Generic;
using Game.Common;
using Game.Rendering;
using Game.Zones;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Zones/", new Type[] { })]
public class ZoneBlockPrefab : PrefabBase
{
	public Material m_Material;

	public ZonePrefab m_ZoneType;

	public override void GetDependencies(List<PrefabBase> prefabs)
	{
		base.GetDependencies(prefabs);
		prefabs.Add(m_ZoneType);
	}

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ZoneBlockData>());
		components.Add(ComponentType.ReadWrite<BatchGroup>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<CurvePosition>());
		components.Add(ComponentType.ReadWrite<ValidArea>());
		components.Add(ComponentType.ReadWrite<BuildOrder>());
		components.Add(ComponentType.ReadWrite<Cell>());
		components.Add(ComponentType.ReadWrite<CullingInfo>());
		components.Add(ComponentType.ReadWrite<MeshBatch>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		List<ComponentBase> list = new List<ComponentBase>();
		GetComponents(list);
		HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
		hashSet.Add(ComponentType.ReadWrite<Block>());
		for (int i = 0; i < list.Count; i++)
		{
			list[i].GetArchetypeComponents(hashSet);
		}
		hashSet.Add(ComponentType.ReadWrite<Created>());
		hashSet.Add(ComponentType.ReadWrite<Updated>());
		entityManager.SetComponentData(entity, new ZoneBlockData
		{
			m_Archetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet))
		});
	}
}
