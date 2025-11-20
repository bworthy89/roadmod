using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct NetCompositionArea : IBufferElementData
{
	public NetAreaFlags m_Flags;

	public float3 m_Position;

	public float m_Width;

	public float3 m_SnapPosition;

	public float m_SnapWidth;
}
