using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Rendering;

[InternalBufferCapacity(1)]
public struct MeshColor : IBufferElementData, IEmptySerializable
{
	public ColorSet m_ColorSet;
}
