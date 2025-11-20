using System;
using System.Collections.Generic;
using Unity.Entities;

namespace Game.Prefabs;

[ComponentMenu("Settings/", new Type[] { })]
public class ToolUXSoundSettingsPrefab : PrefabBase
{
	public PrefabBase m_PolygonToolSelectPointSound;

	public PrefabBase m_PolygonToolDropPointSound;

	public PrefabBase m_PolygonToolRemovePointSound;

	public PrefabBase m_PolygonToolDeleteAreaSound;

	public PrefabBase m_PolygonToolFinishAreaSound;

	public PrefabBase m_BulldozeSound;

	public PrefabBase m_PropPlantBulldozeSound;

	public PrefabBase m_TerraformSound;

	public PrefabBase m_PlaceBuildingSound;

	public PrefabBase m_RelocateBuildingSound;

	public PrefabBase m_PlaceUpgradeSound;

	public PrefabBase m_PlacePropSound;

	public PrefabBase m_PlaceBuildingFailSound;

	public PrefabBase m_ZoningFillSound;

	public PrefabBase m_ZoningRemoveFillSound;

	public PrefabBase m_ZoningStartPaintSound;

	public PrefabBase m_ZoningEndPaintSound;

	public PrefabBase m_ZoningStartRemovePaintSound;

	public PrefabBase m_ZoningEndRemovePaintSound;

	public PrefabBase m_ZoningMarqueeStartSound;

	public PrefabBase m_ZoningMarqueeEndSound;

	public PrefabBase m_ZoningMarqueeClearStartSound;

	public PrefabBase m_ZoningMarqueeClearEndSound;

	public PrefabBase m_SelectEntitySound;

	public PrefabBase m_SnapSound;

	public PrefabBase m_NetExpandSound;

	public PrefabBase m_NetStartSound;

	public PrefabBase m_NetNodeSound;

	public PrefabBase m_NetBuildSound;

	public PrefabBase m_NetCancelSound;

	public PrefabBase m_NetElevationUpSound;

	public PrefabBase m_NetElevationDownSound;

	public PrefabBase m_TransportLineCompleteSound;

	public PrefabBase m_TransportLineStartSound;

	public PrefabBase m_TransportLineBuildSound;

	public PrefabBase m_TransportLineRemoveSound;

	public PrefabBase m_AreaMarqueeStartSound;

	public PrefabBase m_AreaMarqueeEndSound;

	public PrefabBase m_AreaMarqueeClearStartSound;

	public PrefabBase m_AreaMarqueeClearEndSound;

	public PrefabBase m_TutorialStartedSound;

	public PrefabBase m_TutorialCompletedSound;

	public PrefabBase m_CameraZoomInSound;

	public PrefabBase m_CameraZoomOutSound;

	public PrefabBase m_DeletetEntitySound;

	public override void GetPrefabComponents(HashSet<ComponentType> components)
	{
		base.GetPrefabComponents(components);
		components.Add(ComponentType.ReadWrite<ToolUXSoundSettingsData>());
	}

	public override void LateInitialize(EntityManager entityManager, Entity entity)
	{
		base.LateInitialize(entityManager, entity);
		PrefabSystem orCreateSystemManaged = entityManager.World.GetOrCreateSystemManaged<PrefabSystem>();
		ToolUXSoundSettingsData componentData = default(ToolUXSoundSettingsData);
		componentData.m_PolygonToolSelectPointSound = orCreateSystemManaged.GetEntity(m_PolygonToolSelectPointSound);
		componentData.m_PolygonToolDropPointSound = orCreateSystemManaged.GetEntity(m_PolygonToolDropPointSound);
		componentData.m_PolygonToolRemovePointSound = orCreateSystemManaged.GetEntity(m_PolygonToolRemovePointSound);
		componentData.m_PolygonToolDeleteAreaSound = orCreateSystemManaged.GetEntity(m_PolygonToolDeleteAreaSound);
		componentData.m_PolygonToolFinishAreaSound = orCreateSystemManaged.GetEntity(m_PolygonToolFinishAreaSound);
		componentData.m_BulldozeSound = orCreateSystemManaged.GetEntity(m_BulldozeSound);
		componentData.m_PropPlantBulldozeSound = orCreateSystemManaged.GetEntity(m_PropPlantBulldozeSound);
		componentData.m_TerraformSound = orCreateSystemManaged.GetEntity(m_TerraformSound);
		componentData.m_PlaceBuildingSound = orCreateSystemManaged.GetEntity(m_PlaceBuildingSound);
		componentData.m_RelocateBuildingSound = orCreateSystemManaged.GetEntity(m_RelocateBuildingSound);
		componentData.m_PlaceUpgradeSound = orCreateSystemManaged.GetEntity(m_PlaceUpgradeSound);
		componentData.m_PlaceBuildingFailSound = orCreateSystemManaged.GetEntity(m_PlaceBuildingFailSound);
		componentData.m_ZoningFillSound = orCreateSystemManaged.GetEntity(m_ZoningFillSound);
		componentData.m_ZoningRemoveFillSound = orCreateSystemManaged.GetEntity(m_ZoningRemoveFillSound);
		componentData.m_ZoningStartPaintSound = orCreateSystemManaged.GetEntity(m_ZoningStartPaintSound);
		componentData.m_ZoningEndPaintSound = orCreateSystemManaged.GetEntity(m_ZoningEndPaintSound);
		componentData.m_ZoningStartRemovePaintSound = orCreateSystemManaged.GetEntity(m_ZoningStartRemovePaintSound);
		componentData.m_ZoningEndRemovePaintSound = orCreateSystemManaged.GetEntity(m_ZoningEndRemovePaintSound);
		componentData.m_ZoningMarqueeStartSound = orCreateSystemManaged.GetEntity(m_ZoningMarqueeStartSound);
		componentData.m_ZoningMarqueeEndSound = orCreateSystemManaged.GetEntity(m_ZoningMarqueeEndSound);
		componentData.m_ZoningMarqueeClearStartSound = orCreateSystemManaged.GetEntity(m_ZoningMarqueeClearStartSound);
		componentData.m_ZoningMarqueeClearEndSound = orCreateSystemManaged.GetEntity(m_ZoningMarqueeClearEndSound);
		componentData.m_SelectEntitySound = orCreateSystemManaged.GetEntity(m_SelectEntitySound);
		componentData.m_SnapSound = orCreateSystemManaged.GetEntity(m_SnapSound);
		componentData.m_PlacePropSound = orCreateSystemManaged.GetEntity(m_PlacePropSound);
		componentData.m_NetExpandSound = orCreateSystemManaged.GetEntity(m_NetExpandSound);
		componentData.m_NetStartSound = orCreateSystemManaged.GetEntity(m_NetStartSound);
		componentData.m_NetNodeSound = orCreateSystemManaged.GetEntity(m_NetNodeSound);
		componentData.m_NetBuildSound = orCreateSystemManaged.GetEntity(m_NetBuildSound);
		componentData.m_NetCancelSound = orCreateSystemManaged.GetEntity(m_NetCancelSound);
		componentData.m_NetElevationUpSound = orCreateSystemManaged.GetEntity(m_NetElevationUpSound);
		componentData.m_NetElevationDownSound = orCreateSystemManaged.GetEntity(m_NetElevationDownSound);
		componentData.m_TransportLineCompleteSound = orCreateSystemManaged.GetEntity(m_TransportLineCompleteSound);
		componentData.m_TransportLineStartSound = orCreateSystemManaged.GetEntity(m_TransportLineStartSound);
		componentData.m_TransportLineBuildSound = orCreateSystemManaged.GetEntity(m_TransportLineBuildSound);
		componentData.m_TransportLineRemoveSound = orCreateSystemManaged.GetEntity(m_TransportLineRemoveSound);
		componentData.m_AreaMarqueeStartSound = orCreateSystemManaged.GetEntity(m_AreaMarqueeStartSound);
		componentData.m_AreaMarqueeEndSound = orCreateSystemManaged.GetEntity(m_AreaMarqueeEndSound);
		componentData.m_AreaMarqueeClearStartSound = orCreateSystemManaged.GetEntity(m_AreaMarqueeClearStartSound);
		componentData.m_AreaMarqueeClearEndSound = orCreateSystemManaged.GetEntity(m_AreaMarqueeClearEndSound);
		componentData.m_TutorialStartedSound = orCreateSystemManaged.GetEntity(m_TutorialStartedSound);
		componentData.m_TutorialCompletedSound = orCreateSystemManaged.GetEntity(m_TutorialCompletedSound);
		componentData.m_CameraZoomInSound = orCreateSystemManaged.GetEntity(m_CameraZoomInSound);
		componentData.m_CameraZoomOutSound = orCreateSystemManaged.GetEntity(m_CameraZoomOutSound);
		componentData.m_DeletetEntitySound = orCreateSystemManaged.GetEntity(m_DeletetEntitySound);
		entityManager.SetComponentData(entity, componentData);
	}
}
