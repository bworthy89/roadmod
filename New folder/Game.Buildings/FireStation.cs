using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct FireStation : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetRequest;

	public FireStationFlags m_Flags;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		FireStationFlags flags = m_Flags;
		writer.Write((byte)flags);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.reverseServiceRequests2)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
		}
		reader.Read(out byte value);
		m_Flags = (FireStationFlags)value;
	}
}
