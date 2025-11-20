using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Events;

[InternalBufferCapacity(4)]
public struct HotspotFrame : IBufferElementData, IEmptySerializable
{
	public float3 m_Position;

	public float3 m_Velocity;

	public HotspotFrame(WeatherPhenomenon weatherPhenomenon)
	{
		m_Position = weatherPhenomenon.m_HotspotPosition;
		m_Velocity = weatherPhenomenon.m_HotspotVelocity;
	}
}
