using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Areas;

[InternalBufferCapacity(2)]
public struct LabelExtents : IBufferElementData, IEmptySerializable
{
	public Bounds2 m_Bounds;

	public LabelExtents(float2 min, float2 max)
	{
		m_Bounds = new Bounds2(min, max);
	}
}
