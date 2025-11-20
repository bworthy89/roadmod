using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Events;

public struct WeatherPhenomenon : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_PhenomenonPosition;

	public float3 m_HotspotPosition;

	public float3 m_HotspotVelocity;

	public float m_PhenomenonRadius;

	public float m_HotspotRadius;

	public float m_Intensity;

	public float m_LightningTimer;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 phenomenonPosition = m_PhenomenonPosition;
		writer.Write(phenomenonPosition);
		float3 hotspotPosition = m_HotspotPosition;
		writer.Write(hotspotPosition);
		float3 hotspotVelocity = m_HotspotVelocity;
		writer.Write(hotspotVelocity);
		float phenomenonRadius = m_PhenomenonRadius;
		writer.Write(phenomenonRadius);
		float hotspotRadius = m_HotspotRadius;
		writer.Write(hotspotRadius);
		float intensity = m_Intensity;
		writer.Write(intensity);
		float lightningTimer = m_LightningTimer;
		writer.Write(lightningTimer);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 phenomenonPosition = ref m_PhenomenonPosition;
		reader.Read(out phenomenonPosition);
		ref float3 hotspotPosition = ref m_HotspotPosition;
		reader.Read(out hotspotPosition);
		ref float3 hotspotVelocity = ref m_HotspotVelocity;
		reader.Read(out hotspotVelocity);
		ref float phenomenonRadius = ref m_PhenomenonRadius;
		reader.Read(out phenomenonRadius);
		ref float hotspotRadius = ref m_HotspotRadius;
		reader.Read(out hotspotRadius);
		ref float intensity = ref m_Intensity;
		reader.Read(out intensity);
		if (reader.context.version >= Version.lightningSimulation)
		{
			if (reader.context.version < Version.weatherPhenomenonFix)
			{
				reader.Read(out float _);
			}
			ref float lightningTimer = ref m_LightningTimer;
			reader.Read(out lightningTimer);
		}
	}
}
