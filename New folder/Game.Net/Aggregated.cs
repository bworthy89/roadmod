using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct Aggregated : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public Entity m_Aggregate;
}
