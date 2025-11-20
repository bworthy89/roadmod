using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

[InternalBufferCapacity(16)]
public struct SubFlow : IBufferElementData, IEmptySerializable
{
	public sbyte m_Value;
}
