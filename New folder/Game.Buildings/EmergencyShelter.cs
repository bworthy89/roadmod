using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct EmergencyShelter : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetRequest;

	public EmergencyShelterFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		EmergencyShelterFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.reverseServiceRequests)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
		}
		reader.Read(out byte value);
		m_Flags = (EmergencyShelterFlags)value;
	}
}
