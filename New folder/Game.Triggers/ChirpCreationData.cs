using Unity.Entities;

namespace Game.Triggers;

public struct ChirpCreationData
{
	public Entity m_TriggerPrefab;

	public Entity m_Sender;

	public Entity m_Target;
}
