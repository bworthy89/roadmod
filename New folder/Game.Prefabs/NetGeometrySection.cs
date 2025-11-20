using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Prefabs;

[InternalBufferCapacity(0)]
public struct NetGeometrySection : IBufferElementData, IEmptySerializable
{
	public Entity m_Section;

	public CompositionFlags m_CompositionAll;

	public CompositionFlags m_CompositionAny;

	public CompositionFlags m_CompositionNone;

	public NetSectionFlags m_Flags;

	public float3 m_Offset;
}
