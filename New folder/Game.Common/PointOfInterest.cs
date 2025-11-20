using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Common;

public struct PointOfInterest : IComponentData, IQueryTypeParameter, ISerializable
{
	public float3 m_Position;

	public bool m_IsValid;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 position = m_Position;
		writer.Write(position);
		bool isValid = m_IsValid;
		writer.Write(isValid);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 position = ref m_Position;
		reader.Read(out position);
		ref bool isValid = ref m_IsValid;
		reader.Read(out isValid);
	}
}
