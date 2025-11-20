using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

public struct RandomLikeCountData : IComponentData, IQueryTypeParameter
{
	public float m_EducatedPercentage;

	public float m_UneducatedPercentage;

	public float2 m_RandomAmountFactor;

	public float2 m_ActiveDays;

	public float m_ContinuousFactor;

	public int2 m_GoViralFactor;
}
