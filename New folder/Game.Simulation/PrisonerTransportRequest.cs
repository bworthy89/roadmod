using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct PrisonerTransportRequest : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Target;

	public int m_Priority;

	public PrisonerTransportRequest(Entity target, int priority)
	{
		m_Target = target;
		m_Priority = priority;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity target = m_Target;
		writer.Write(target);
		int priority = m_Priority;
		writer.Write(priority);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity target = ref m_Target;
		reader.Read(out target);
		ref int priority = ref m_Priority;
		reader.Read(out priority);
	}
}
