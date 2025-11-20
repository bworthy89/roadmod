using Game.Triggers;
using Unity.Entities;

namespace Game.Prefabs;

public struct TriggerData : IBufferElementData
{
	public TriggerType m_TriggerType;

	public TargetType m_TargetTypes;

	public Entity m_TriggerPrefab;
}
