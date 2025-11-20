using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct BuildingConfigurationData : IComponentData, IQueryTypeParameter
{
	public int3 m_BuildingConditionIncrement;

	public int m_BuildingConditionDecrement;

	public Entity m_AbandonedBuildingLocalEffects;

	public Entity m_AbandonedCollapsedBuildingLocalEffects;

	public Entity m_AbandonedCollapsedNotification;

	public Entity m_AbandonedNotification;

	public Entity m_CondemnedNotification;

	public Entity m_LevelUpNotification;

	public Entity m_TurnedOffNotification;

	public Entity m_ElectricityConnectionLane;

	public Entity m_SewageConnectionLane;

	public Entity m_WaterConnectionLane;

	public uint m_AbandonedDestroyDelay;

	public Entity m_HighRentNotification;

	public Entity m_DefaultRenterBrand;

	public Entity m_ConstructionSurface;

	public Entity m_ConstructionBorder;

	public Entity m_ConstructionObject;

	public Entity m_CollapsedObject;

	public Entity m_CollapseVFX;

	public Entity m_CollapseSFX;

	public float m_CollapseSFXDensity;

	public Entity m_CollapsedSurface;

	public Entity m_FireLoopSFX;

	public Entity m_FireSpotSFX;

	public Entity m_LevelingBuildingNotificationPrefab;
}
