using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct TrainNavigation : IComponentData, IQueryTypeParameter, ISerializable
{
	public TrainBogiePosition m_Front;

	public TrainBogiePosition m_Rear;

	public float m_Speed;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		TrainBogiePosition front = m_Front;
		writer.Write(front);
		TrainBogiePosition rear = m_Rear;
		writer.Write(rear);
		float speed = m_Speed;
		writer.Write(speed);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref TrainBogiePosition front = ref m_Front;
		reader.Read(out front);
		ref TrainBogiePosition rear = ref m_Rear;
		reader.Read(out rear);
		ref float speed = ref m_Speed;
		reader.Read(out speed);
	}
}
