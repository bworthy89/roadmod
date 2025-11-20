using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct NetCompositionCarriageway : IBufferElementData
{
	public float3 m_Position;

	public float m_Width;

	public LaneFlags m_Flags;
}
