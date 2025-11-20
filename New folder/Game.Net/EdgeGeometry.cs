using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct EdgeGeometry : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public Segment m_Start;

	public Segment m_End;

	public Bounds3 m_Bounds;
}
