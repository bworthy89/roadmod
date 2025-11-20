using Unity.Entities;

namespace Game.Prefabs;

public struct SoilWaterParameterData : IComponentData, IQueryTypeParameter
{
	public float m_RainMultiplier;

	public float m_HeightEffect;

	public float m_MaxDiffusion;

	public float m_WaterPerUnit;

	public float m_MoistureUnderWater;

	public float m_MaximumWaterDepth;

	public float m_OverflowRate;
}
