using System;
using Unity.Entities;

namespace Game.Prefabs.Modes;

[ComponentMenu("Modes/Mode Prefab/", new Type[] { })]
public class MapTileMode : LocalModePrefab
{
	public MapTilePrefab m_Prefab;

	public MapTilePrefab.FeatureInfo[] m_MapFeatures;

	public override void RecordChanges(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		MapTilePrefab mapTilePrefab = m_Prefab;
		if (mapTilePrefab == null)
		{
			ComponentBase.baseLog.Critical($"Target not found {this}");
			return;
		}
		Entity entity = prefabSystem.GetEntity(mapTilePrefab);
		entityManager.GetComponentData<TilePurchaseCostFactor>(entity);
		entityManager.GetBuffer<MapFeatureData>(entity);
	}

	public override void ApplyModeData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		MapTilePrefab mapTilePrefab = m_Prefab;
		if (mapTilePrefab == null)
		{
			ComponentBase.baseLog.Critical($"Target not found {this}");
			return;
		}
		Entity entity = prefabSystem.GetEntity(mapTilePrefab);
		DynamicBuffer<MapFeatureData> buffer = entityManager.GetBuffer<MapFeatureData>(entity);
		for (int i = 0; i < m_MapFeatures.Length; i++)
		{
			MapTilePrefab.FeatureInfo featureInfo = m_MapFeatures[i];
			buffer[(int)featureInfo.m_MapFeature] = new MapFeatureData(featureInfo.m_Cost);
		}
		TilePurchaseCostFactor componentData = new TilePurchaseCostFactor(mapTilePrefab.m_PurchaseCostFactor);
		entityManager.SetComponentData(entity, componentData);
	}

	public override void RestoreDefaultData(EntityManager entityManager, PrefabSystem prefabSystem)
	{
		MapTilePrefab mapTilePrefab = m_Prefab;
		if (mapTilePrefab == null)
		{
			ComponentBase.baseLog.Critical($"Target not found {this}");
			return;
		}
		Entity entity = prefabSystem.GetEntity(mapTilePrefab);
		DynamicBuffer<MapFeatureData> buffer = entityManager.GetBuffer<MapFeatureData>(entity);
		for (int i = 0; i < mapTilePrefab.m_MapFeatures.Length; i++)
		{
			MapTilePrefab.FeatureInfo featureInfo = mapTilePrefab.m_MapFeatures[i];
			buffer[(int)featureInfo.m_MapFeature] = new MapFeatureData(featureInfo.m_Cost);
		}
		TilePurchaseCostFactor componentData = new TilePurchaseCostFactor(mapTilePrefab.m_PurchaseCostFactor);
		entityManager.SetComponentData(entity, componentData);
	}
}
