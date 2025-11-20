using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Common;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Areas/", new Type[] { })]
public class AreaPrefab : PrefabBase
{
	public Color m_Color = Color.white;

	public Color m_EdgeColor = Color.white;

	public Color m_SelectionColor = Color.white;

	public Color m_SelectionEdgeColor = Color.white;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<AreaData>());
		components.Add(ComponentType.ReadWrite<AreaColorData>());
		components.Add(ComponentType.ReadWrite<PlaceableInfoviewItem>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<Node>());
		components.Add(ComponentType.ReadWrite<Triangle>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		List<ComponentBase> list = new List<ComponentBase>();
		GetComponents(list);
		HashSet<ComponentType> hashSet = new HashSet<ComponentType>();
		hashSet.Add(ComponentType.ReadWrite<Area>());
		for (int i = 0; i < list.Count; i++)
		{
			list[i].GetArchetypeComponents(hashSet);
		}
		hashSet.Add(ComponentType.ReadWrite<Created>());
		hashSet.Add(ComponentType.ReadWrite<Updated>());
		AreaData componentData = entityManager.GetComponentData<AreaData>(entity);
		componentData.m_Archetype = entityManager.CreateArchetype(PrefabUtils.ToArray(hashSet));
		entityManager.SetComponentData(entity, componentData);
	}
}
