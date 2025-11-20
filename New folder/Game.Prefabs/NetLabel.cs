using System;
using System.Collections.Generic;
using Game.Net;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Net/", new Type[] { typeof(AggregateNetPrefab) })]
public class NetLabel : ComponentBase
{
	public Material m_NameMaterial;

	public Color m_NameColor = Color.white;

	public Color m_SelectedNameColor = new Color(0.5f, 0.75f, 1f, 1f);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<NetNameData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<LabelMaterial>());
		components.Add(ComponentType.ReadWrite<LabelExtents>());
		components.Add(ComponentType.ReadWrite<LabelPosition>());
		components.Add(ComponentType.ReadWrite<LabelVertex>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new NetNameData
		{
			m_Color = m_NameColor.linear,
			m_SelectedColor = m_SelectedNameColor.linear
		});
	}
}
