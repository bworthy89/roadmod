using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct SecondaryNetLane : IBufferElementData
{
	public Entity m_Lane;

	public SecondaryNetLaneFlags m_Flags;
}
