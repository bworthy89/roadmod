using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(4)]
public struct SubAreaNode : IBufferElementData
{
	public float3 m_Position;

	public int m_ParentMesh;

	public SubAreaNode(float3 position, int parentMesh)
	{
		m_Position = position;
		m_ParentMesh = parentMesh;
	}
}
