using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct MeshMaterial : IBufferElementData
{
	public int m_StartIndex;

	public int m_IndexCount;

	public int m_StartVertex;

	public int m_VertexCount;

	public int m_MaterialIndex;

	public MeshMaterial(int startIndex, int indexCount, int startVertex, int vertexCount, int materialIndex)
	{
		m_StartIndex = startIndex;
		m_IndexCount = indexCount;
		m_StartVertex = startVertex;
		m_VertexCount = vertexCount;
		m_MaterialIndex = materialIndex;
	}
}
