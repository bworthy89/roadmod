using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct GarbageTruck : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetRequest;

	public GarbageTruckFlags m_State;

	public int m_RequestCount;

	public int m_Garbage;

	public int m_EstimatedGarbage;

	public float m_PathElementTime;

	public GarbageTruck(GarbageTruckFlags flags, int requestCount)
	{
		m_TargetRequest = Entity.Null;
		m_State = flags;
		m_RequestCount = requestCount;
		m_Garbage = 0;
		m_EstimatedGarbage = 0;
		m_PathElementTime = 0f;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		int estimatedGarbage = m_EstimatedGarbage;
		writer.Write(estimatedGarbage);
		GarbageTruckFlags state = m_State;
		writer.Write((uint)state);
		int requestCount = m_RequestCount;
		writer.Write(requestCount);
		int garbage = m_Garbage;
		writer.Write(garbage);
		float pathElementTime = m_PathElementTime;
		writer.Write(pathElementTime);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.reverseServiceRequests2)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
			ref int estimatedGarbage = ref m_EstimatedGarbage;
			reader.Read(out estimatedGarbage);
		}
		reader.Read(out uint value);
		ref int requestCount = ref m_RequestCount;
		reader.Read(out requestCount);
		ref int garbage = ref m_Garbage;
		reader.Read(out garbage);
		ref float pathElementTime = ref m_PathElementTime;
		reader.Read(out pathElementTime);
		m_State = (GarbageTruckFlags)value;
	}
}
