using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Net;

public struct NodeGeometry : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public Bounds3 m_Bounds;

	public float m_Position;

	public float m_Flatness;

	public float m_Offset;
}
