using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Events;

public struct WaterLevelChange : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_Intensity;

	public float m_MaxIntensity;

	public float m_DangerHeight;

	public float2 m_Direction;

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float intensity = ref m_Intensity;
		reader.Read(out intensity);
		ref float maxIntensity = ref m_MaxIntensity;
		reader.Read(out maxIntensity);
		ref float dangerHeight = ref m_DangerHeight;
		reader.Read(out dangerHeight);
		if (reader.context.version >= Version.tsunamiDirection)
		{
			reader.Read(out float2 _);
		}
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float intensity = m_Intensity;
		writer.Write(intensity);
		float maxIntensity = m_MaxIntensity;
		writer.Write(maxIntensity);
		float dangerHeight = m_DangerHeight;
		writer.Write(dangerHeight);
		float2 direction = m_Direction;
		writer.Write(direction);
	}
}
