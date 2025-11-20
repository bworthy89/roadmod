using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(2)]
public struct LodMesh : IBufferElementData, IEmptySerializable
{
	public Entity m_LodMesh;
}
