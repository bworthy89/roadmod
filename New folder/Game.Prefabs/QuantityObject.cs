using System;
using System.Collections.Generic;
using Game.Areas;
using Game.Economy;
using Game.Objects;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Objects/", new Type[] { typeof(StaticObjectPrefab) })]
public class QuantityObject : ComponentBase
{
	public ResourceInEditor[] m_Resources;

	public MapFeature m_MapFeature = MapFeature.None;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<QuantityObjectData>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		components.Add(ComponentType.ReadWrite<Quantity>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		QuantityObjectData componentData = default(QuantityObjectData);
		if (m_Resources != null)
		{
			for (int i = 0; i < m_Resources.Length; i++)
			{
				componentData.m_Resources |= EconomyUtils.GetResource(m_Resources[i]);
			}
		}
		componentData.m_MapFeature = m_MapFeature;
		entityManager.SetComponentData(entity, componentData);
		if (componentData.m_Resources == Resource.NoResource && componentData.m_MapFeature == MapFeature.None && !base.prefab.Has<PlaceholderObject>())
		{
			ComponentBase.baseLog.WarnFormat(base.prefab, "QuantityObject has no resource: {0}", base.prefab.name);
		}
	}
}
