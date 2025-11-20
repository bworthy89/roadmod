using Colossal.Mathematics;
using Colossal.Serialization.Entities;
using Unity.Entities;
using Unity.Mathematics;

namespace Game.Creatures;

public struct Creature : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_QueueEntity;

	public Sphere3 m_QueueArea;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		float radius = m_QueueArea.radius;
		writer.Write(radius);
		if (m_QueueArea.radius > 0f)
		{
			Entity queueEntity = m_QueueEntity;
			writer.Write(queueEntity);
			float3 position = m_QueueArea.position;
			writer.Write(position);
		}
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref float radius = ref m_QueueArea.radius;
		reader.Read(out radius);
		if (m_QueueArea.radius > 0f)
		{
			ref Entity queueEntity = ref m_QueueEntity;
			reader.Read(out queueEntity);
			ref float3 position = ref m_QueueArea.position;
			reader.Read(out position);
		}
	}
}
