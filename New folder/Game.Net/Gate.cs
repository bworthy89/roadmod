using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct Gate : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public Entity m_Domain;
}
