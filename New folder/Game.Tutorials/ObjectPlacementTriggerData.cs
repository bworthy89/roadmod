using Unity.Entities;

namespace Game.Tutorials;

public struct ObjectPlacementTriggerData : IBufferElementData
{
	public Entity m_Object;

	public ObjectPlacementTriggerFlags m_Flags;

	public ObjectPlacementTriggerData(Entity obj, ObjectPlacementTriggerFlags flags)
	{
		m_Object = obj;
		m_Flags = flags;
	}
}
