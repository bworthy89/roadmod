using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Rendering;

[InternalBufferCapacity(1)]
public struct MeshBatch : IBufferElementData, IEmptySerializable
{
	public int m_GroupIndex;

	public int m_InstanceIndex;

	public byte m_MeshGroup;

	public byte m_MeshIndex;

	public byte m_TileIndex;
}
