using Colossal.Serialization.Entities;
using Game.Objects;
using Unity.Mathematics;

namespace Game.Vehicles;

public struct TrainBogiePosition : ISerializable
{
	public float3 m_Position;

	public float3 m_Direction;

	public TrainBogiePosition(Transform transform)
	{
		m_Position = transform.m_Position;
		m_Direction = math.forward(transform.m_Rotation);
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float3 position = m_Position;
		writer.Write(position);
		float3 direction = m_Direction;
		writer.Write(direction);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float3 position = ref m_Position;
		reader.Read(out position);
		ref float3 direction = ref m_Direction;
		reader.Read(out direction);
	}
}
