using Colossal.Mathematics;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct MeshNode : IBufferElementData
{
	public Bounds3 m_Bounds;

	public int2 m_IndexRange;

	public int4 m_SubNodes1;

	public int4 m_SubNodes2;
}
