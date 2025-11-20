using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct Roundabout : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public float m_Radius;
}
