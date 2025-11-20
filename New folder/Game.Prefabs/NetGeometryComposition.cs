using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct NetGeometryComposition : IBufferElementData, IEmptySerializable
{
	public Entity m_Composition;

	public CompositionFlags m_Mask;
}
