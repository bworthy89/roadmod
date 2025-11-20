using Unity.Entities;
using Unity.Mathematics;

namespace Game.Tools;

[InternalBufferCapacity(0)]
public struct LocalNodeCache : IBufferElementData
{
	public float3 m_Position;

	public int m_ParentMesh;
}
