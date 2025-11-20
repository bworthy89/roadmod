using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct EvacuationRequest : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Target;

	public float m_Priority;

	public EvacuationRequest(Entity target, float priority)
	{
		m_Target = target;
		m_Priority = priority;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity target = m_Target;
		writer.Write(target);
		float priority = m_Priority;
		writer.Write(priority);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity target = ref m_Target;
		reader.Read(out target);
		ref float priority = ref m_Priority;
		reader.Read(out priority);
	}
}
