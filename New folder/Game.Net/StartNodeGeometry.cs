using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct StartNodeGeometry : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public EdgeNodeGeometry m_Geometry;
}
