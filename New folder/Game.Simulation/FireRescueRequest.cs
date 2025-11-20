using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct FireRescueRequest : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Target;

	public float m_Priority;

	public FireRescueRequestType m_Type;

	public FireRescueRequest(Entity target, float priority, FireRescueRequestType type)
	{
		m_Target = target;
		m_Priority = priority;
		m_Type = type;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity target = m_Target;
		writer.Write(target);
		float priority = m_Priority;
		writer.Write(priority);
		FireRescueRequestType type = m_Type;
		writer.Write((byte)type);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity target = ref m_Target;
		reader.Read(out target);
		ref float priority = ref m_Priority;
		reader.Read(out priority);
		if (reader.context.version >= Version.disasterResponse)
		{
			reader.Read(out byte value);
			m_Type = (FireRescueRequestType)value;
		}
	}
}
