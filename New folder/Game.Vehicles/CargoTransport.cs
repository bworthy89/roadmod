using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct CargoTransport : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetRequest;

	public CargoTransportFlags m_State;

	public uint m_DepartureFrame;

	public int m_RequestCount;

	public float m_PathElementTime;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		CargoTransportFlags state = m_State;
		writer.Write((uint)state);
		uint departureFrame = m_DepartureFrame;
		writer.Write(departureFrame);
		int requestCount = m_RequestCount;
		writer.Write(requestCount);
		float pathElementTime = m_PathElementTime;
		writer.Write(pathElementTime);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.reverseServiceRequests)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
		}
		reader.Read(out uint value);
		ref uint departureFrame = ref m_DepartureFrame;
		reader.Read(out departureFrame);
		if (reader.context.version >= Version.evacuationTransport)
		{
			ref int requestCount = ref m_RequestCount;
			reader.Read(out requestCount);
			ref float pathElementTime = ref m_PathElementTime;
			reader.Read(out pathElementTime);
		}
		m_State = (CargoTransportFlags)value;
	}
}
