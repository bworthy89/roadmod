using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct PolicePatrolRequest : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Target;

	public float m_Priority;

	public byte m_DispatchIndex;

	public PolicePatrolRequest(Entity target, float priority)
	{
		m_Target = target;
		m_Priority = priority;
		m_DispatchIndex = 0;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity target = m_Target;
		writer.Write(target);
		float priority = m_Priority;
		writer.Write(priority);
		byte dispatchIndex = m_DispatchIndex;
		writer.Write(dispatchIndex);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity target = ref m_Target;
		reader.Read(out target);
		ref float priority = ref m_Priority;
		reader.Read(out priority);
		if (reader.context.version >= Version.requestDispatchIndex)
		{
			ref byte dispatchIndex = ref m_DispatchIndex;
			reader.Read(out dispatchIndex);
		}
	}
}
