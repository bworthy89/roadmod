using Colossal.Mathematics;
using Unity.Entities;

namespace Game.Prefabs;

public struct CalendarEventData : IComponentData, IQueryTypeParameter
{
	public EventTargetType m_RandomTargetType;

	public CalendarEventMonths m_AllowedMonths;

	public CalendarEventTimes m_AllowedTimes;

	public Bounds1 m_OccurenceProbability;

	public Bounds1 m_AffectedProbability;

	public int m_Duration;
}
