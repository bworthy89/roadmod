using System;
using System.Collections.Generic;
using Colossal.Collections;
using Game.Areas;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Areas/", new Type[] { })]
public class MapTilePrefab : AreaPrefab
{
	[Serializable]
	public class FeatureInfo
	{
		public MapFeature m_MapFeature;

		public float m_Cost;
	}

	public float m_PurchaseCostFactor = 2500f;

	public FeatureInfo[] m_MapFeatures;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<MapTileData>());
		components.Add(ComponentType.ReadWrite<MapFeatureData>());
		components.Add(ComponentType.ReadWrite<AreaGeometryData>());
		components.Add(ComponentType.ReadWrite<TilePurchaseCostFactor>());
	}

	public override void GetArchetypeComponents(HashSet<ComponentType> components)
	{
		base.GetArchetypeComponents(components);
		components.Add(ComponentType.ReadWrite<MapTile>());
		components.Add(ComponentType.ReadWrite<MapFeatureElement>());
		components.Add(ComponentType.ReadWrite<Geometry>());
	}

	public override void Initialize(EntityManager entityManager, Entity entity)
	{
		base.Initialize(entityManager, entity);
		DynamicBuffer<MapFeatureData> buffer = entityManager.GetBuffer<MapFeatureData>(entity);
		CollectionUtils.ResizeInitialized(buffer, 9);
		for (int i = 0; i < m_MapFeatures.Length; i++)
		{
			FeatureInfo featureInfo = m_MapFeatures[i];
			buffer[(int)featureInfo.m_MapFeature] = new MapFeatureData(featureInfo.m_Cost);
		}
		TilePurchaseCostFactor componentData = new TilePurchaseCostFactor(m_PurchaseCostFactor);
		entityManager.SetComponentData(entity, componentData);
	}
}
