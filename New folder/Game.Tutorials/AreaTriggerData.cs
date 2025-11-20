using Unity.Entities;

namespace Game.Tutorials;

public struct AreaTriggerData : IBufferElementData
{
	public Entity m_Prefab;

	public AreaTriggerFlags m_Flags;

	public AreaTriggerData(Entity prefab, AreaTriggerFlags flags)
	{
		m_Prefab = prefab;
		m_Flags = flags;
	}
}
