using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct FireEngine : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetRequest;

	public FireEngineFlags m_State;

	public int m_RequestCount;

	public float m_PathElementTime;

	public float m_ExtinguishingAmount;

	public float m_Efficiency;

	public FireEngine(FireEngineFlags state, int requestCount, float extinguishingAmount, float efficiency)
	{
		m_TargetRequest = Entity.Null;
		m_State = state;
		m_RequestCount = requestCount;
		m_PathElementTime = 0f;
		m_ExtinguishingAmount = extinguishingAmount;
		m_Efficiency = efficiency;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		FireEngineFlags state = m_State;
		writer.Write((uint)state);
		int requestCount = m_RequestCount;
		writer.Write(requestCount);
		float pathElementTime = m_PathElementTime;
		writer.Write(pathElementTime);
		float extinguishingAmount = m_ExtinguishingAmount;
		writer.Write(extinguishingAmount);
		float efficiency = m_Efficiency;
		writer.Write(efficiency);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.reverseServiceRequests2)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
		}
		reader.Read(out uint value);
		ref int requestCount = ref m_RequestCount;
		reader.Read(out requestCount);
		ref float pathElementTime = ref m_PathElementTime;
		reader.Read(out pathElementTime);
		if (reader.context.version >= Version.aircraftNavigation)
		{
			ref float extinguishingAmount = ref m_ExtinguishingAmount;
			reader.Read(out extinguishingAmount);
		}
		if (reader.context.version >= Version.disasterResponse)
		{
			ref float efficiency = ref m_Efficiency;
			reader.Read(out efficiency);
		}
		else
		{
			m_Efficiency = 1f;
		}
		m_State = (FireEngineFlags)value;
	}
}
