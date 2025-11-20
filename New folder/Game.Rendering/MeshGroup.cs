using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Rendering;

[InternalBufferCapacity(1)]
public struct MeshGroup : IBufferElementData, IEmptySerializable
{
	public ushort m_SubMeshGroup;

	public byte m_MeshOffset;

	public byte m_ColorOffset;
}
