using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Policies;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Areas/", new Type[] { })]
public class DistrictPrefab : AreaPrefab
{
	public Color m_NameColor = Color.white;

	public Color m_SelectedNameColor = new Color(0.5f, 0.75f, 1f, 1f);

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<DistrictData>());
		components.Add(ComponentType.ReadWrite<AreaNameData>());
		components.Add(ComponentType.ReadWrite<AreaGeometryData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<District>());
		components.Add(ComponentType.ReadWrite<Geometry>());
		components.Add(ComponentType.ReadWrite<LabelExtents>());
		components.Add(ComponentType.ReadWrite<LabelVertex>());
		components.Add(ComponentType.ReadWrite<DistrictModifier>());
		components.Add(ComponentType.ReadWrite<Policy>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		entityManager.SetComponentData(entity, new AreaNameData
		{
			m_Color = m_NameColor,
			m_SelectedColor = m_SelectedNameColor
		});
	}
}
