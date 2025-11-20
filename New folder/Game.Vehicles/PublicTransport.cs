using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct PublicTransport : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetRequest;

	public PublicTransportFlags m_State;

	public uint m_DepartureFrame;

	public int m_RequestCount;

	public float m_PathElementTime;

	public float m_MaxBoardingDistance;

	public float m_MinWaitingDistance;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		PublicTransportFlags state = m_State;
		writer.Write((uint)state);
		uint departureFrame = m_DepartureFrame;
		writer.Write(departureFrame);
		int requestCount = m_RequestCount;
		writer.Write(requestCount);
		float pathElementTime = m_PathElementTime;
		writer.Write(pathElementTime);
		float maxBoardingDistance = m_MaxBoardingDistance;
		writer.Write(maxBoardingDistance);
		float minWaitingDistance = m_MinWaitingDistance;
		writer.Write(minWaitingDistance);
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
		if (reader.context.version >= Version.roadPatchImprovements)
		{
			ref float maxBoardingDistance = ref m_MaxBoardingDistance;
			reader.Read(out maxBoardingDistance);
			ref float minWaitingDistance = ref m_MinWaitingDistance;
			reader.Read(out minWaitingDistance);
		}
		m_State = (PublicTransportFlags)value;
	}
}
