using Unity.Entities;

namespace Game.Prefabs;

public struct InitialResourceData : IBufferElementData
{
	public ResourceStack m_Value;
}
