using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Simulation;

public struct MaintenanceConsumer : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_Request;

	public byte m_DispatchIndex;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity request = m_Request;
		writer.Write(request);
		byte dispatchIndex = m_DispatchIndex;
		writer.Write(dispatchIndex);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref Entity request = ref m_Request;
		reader.Read(out request);
		if (reader.context.version >= Version.requestDispatchIndex)
		{
			ref byte dispatchIndex = ref m_DispatchIndex;
			reader.Read(out dispatchIndex);
		}
	}
}
