using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct WaterPowered : IComponentData, IQueryTypeParameter, ISerializable
{
	public float m_Length;

	public float m_Height;

	public float m_Estimate;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float length = m_Length;
		writer.Write(length);
		float height = m_Height;
		writer.Write(height);
		float estimate = m_Estimate;
		writer.Write(estimate);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float length = ref m_Length;
		reader.Read(out length);
		ref float height = ref m_Height;
		reader.Read(out height);
		ref float estimate = ref m_Estimate;
		reader.Read(out estimate);
	}
}
