using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct SubMeshGroup : IBufferElementData
{
	public int m_SubGroupCount;

	public int2 m_SubMeshRange;

	public MeshGroupFlags m_Flags;
}
