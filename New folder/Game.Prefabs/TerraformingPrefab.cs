using System;
using System.Collections.Generic;
using Unity.Entities;
using UnityEngine;

namespace Game.Prefabs;

[ComponentMenu("Tools/", new Type[] { })]
public class TerraformingPrefab : PrefabBase
{
	public TerraformingType m_Type;

	public TerraformingTarget m_Target;

	public Material m_BrushMaterial;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<TerraformingData>());
		components.Add(ComponentType.ReadWrite<PlaceableInfoviewItem>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		TerraformingData componentData = default(TerraformingData);
		componentData.m_Type = m_Type;
		componentData.m_Target = m_Target;
		entityManager.SetComponentData(entity, componentData);
	}
}
