using Unity.Entities;

namespace Game.Triggers;

public struct LifePathEventCreationData
{
	public TriggerType m_TriggerType;

	public Entity m_EventPrefab;

	public Entity m_Sender;

	public Entity m_Target;

	public Entity m_OriginalSender;
}
