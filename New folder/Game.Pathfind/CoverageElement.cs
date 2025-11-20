using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Pathfind;

[InternalBufferCapacity(0)]
public struct CoverageElement : IBufferElementData, IEmptySerializable
{
	public Entity m_Edge;

	public float2 m_Cost;
}
