using Game.Events;
using Unity.Entities;

namespace Game.Prefabs;

public struct WaterLevelChangeData : IComponentData, IQueryTypeParameter
{
	public WaterLevelTargetType m_TargetType;

	public WaterLevelChangeType m_ChangeType;

	public float m_EscalationDelay;

	public DangerFlags m_DangerFlags;

	public float m_DangerLevel;
}
