using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Events;

public struct Flooded : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Event;

	public float m_Depth;

	public Flooded(Entity _event, float depth)
	{
		m_Event = _event;
		m_Depth = depth;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity value = m_Event;
		writer.Write(value);
		float depth = m_Depth;
		writer.Write(depth);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity value = ref m_Event;
		reader.Read(out value);
		ref float depth = ref m_Depth;
		reader.Read(out depth);
	}
}
