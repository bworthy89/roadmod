using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Common;
using Game.Objects;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Areas/", new Type[] { })]
public class LotPrefab : AreaPrefab
{
	public float m_MaxRadius = 200f;

	public UnityEngine.Color m_RangeColor = UnityEngine.Color.white;

	public bool m_OnWater;

	public bool m_AllowOverlap;

	public bool m_AllowEditing = true;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<LotData>());
		components.Add(ComponentType.ReadWrite<AreaGeometryData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<Lot>());
		components.Add(ComponentType.ReadWrite<Geometry>());
		components.Add(ComponentType.ReadWrite<Game.Objects.SubObject>());
		components.Add(ComponentType.ReadWrite<PseudoRandomSeed>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new LotData(m_MaxRadius, m_RangeColor, m_OnWater, m_AllowOverlap, m_AllowEditing));
	}
}
