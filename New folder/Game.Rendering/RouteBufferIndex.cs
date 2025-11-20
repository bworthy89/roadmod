using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Rendering;

public struct RouteBufferIndex : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public int m_Index;
}
