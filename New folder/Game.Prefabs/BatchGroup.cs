using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(4)]
public struct BatchGroup : IBufferElementData
{
	public int m_GroupIndex;

	public int m_MergeIndex;

	public MeshLayer m_Layer;

	public MeshType m_Type;

	public ushort m_Partition;
}
