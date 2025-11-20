using Unity.Entities;

namespace Game.Tutorials;

public struct ZoningTriggerData : IBufferElementData
{
	public Entity m_Zone;

	public ZoningTriggerData(Entity zone)
	{
		m_Zone = zone;
	}
}
