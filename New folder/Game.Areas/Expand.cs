using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Areas;

[InternalBufferCapacity(4)]
public struct Expand : IBufferElementData, IEmptySerializable
{
	public float2 m_Offset;

	public Expand(float2 offset)
	{
		m_Offset = offset;
	}
}
