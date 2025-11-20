using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct Composition : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public Entity m_Edge;

	public Entity m_StartNode;

	public Entity m_EndNode;
}
