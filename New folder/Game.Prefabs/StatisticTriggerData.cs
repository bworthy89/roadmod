using Unity.Entities;

namespace Game.Prefabs;

public struct StatisticTriggerData : IComponentData, IQueryTypeParameter
{
	public StatisticTriggerType m_Type;

	public Entity m_StatisticEntity;

	public int m_StatisticParameter;

	public Entity m_NormalizeWithPrefab;

	public int m_NormalizeWithParameter;

	public int m_TimeFrame;

	public int m_MinSamples;
}
