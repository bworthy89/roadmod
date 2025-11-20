using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Net;

public struct EdgeMapping : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public Entity m_Parent1;

	public Entity m_Parent2;

	public float2 m_CurveDelta1;

	public float2 m_CurveDelta2;
}
