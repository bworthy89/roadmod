using Unity.Entities;

namespace Game.Prefabs;

public struct ZoneServiceConsumptionData : IComponentData, IQueryTypeParameter
{
	public float m_Upkeep;

	public float m_ElectricityConsumption;

	public float m_WaterConsumption;

	public float m_GarbageAccumulation;

	public float m_TelecomNeed;
}
