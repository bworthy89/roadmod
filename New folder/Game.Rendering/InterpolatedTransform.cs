using Colossal.Serialization.Entities;
using Game.Events;
using Game.Objects;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Rendering;

public struct InterpolatedTransform : IComponentData, IQueryTypeParameter, IEmptySerializable
{
	public float3 m_Position;

	public quaternion m_Rotation;

	public TransformFlags m_Flags;

	public InterpolatedTransform(Transform transform)
	{
		m_Position = transform.m_Position;
		m_Rotation = transform.m_Rotation;
		m_Flags = (TransformFlags)0u;
	}

	public InterpolatedTransform(WeatherPhenomenon weatherPhenomenon)
	{
		m_Position = weatherPhenomenon.m_HotspotPosition;
		m_Rotation = quaternion.identity;
		m_Flags = (TransformFlags)0u;
	}

	public Transform ToTransform()
	{
		return new Transform(m_Position, m_Rotation);
	}
}
