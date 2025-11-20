using Colossal.Serialization.Entities;
using Game.Prefabs;
using Unity.Entities;

namespace Game.Vehicles;

public struct PoliceCar : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetRequest;

	public PoliceCarFlags m_State;

	public int m_RequestCount;

	public float m_PathElementTime;

	public uint m_ShiftTime;

	public uint m_EstimatedShift;

	public PolicePurpose m_PurposeMask;

	public PoliceCar(PoliceCarFlags flags, int requestCount, PolicePurpose purposeMask)
	{
		m_TargetRequest = Entity.Null;
		m_State = flags;
		m_RequestCount = requestCount;
		m_PathElementTime = 0f;
		m_ShiftTime = 0u;
		m_EstimatedShift = 0u;
		m_PurposeMask = purposeMask;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		PoliceCarFlags state = m_State;
		writer.Write((uint)state);
		int requestCount = m_RequestCount;
		writer.Write(requestCount);
		float pathElementTime = m_PathElementTime;
		writer.Write(pathElementTime);
		uint shiftTime = m_ShiftTime;
		writer.Write(shiftTime);
		uint estimatedShift = m_EstimatedShift;
		writer.Write(estimatedShift);
		PolicePurpose purposeMask = m_PurposeMask;
		writer.Write((int)purposeMask);
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
		ref uint shiftTime = ref m_ShiftTime;
		reader.Read(out shiftTime);
		if (reader.context.version >= Version.policeShiftEstimate)
		{
			ref uint estimatedShift = ref m_EstimatedShift;
			reader.Read(out estimatedShift);
		}
		m_State = (PoliceCarFlags)value;
		if (reader.context.version >= Version.policeImprovement3)
		{
			reader.Read(out int value2);
			m_PurposeMask = (PolicePurpose)value2;
		}
		else
		{
			m_PurposeMask = PolicePurpose.Patrol | PolicePurpose.Emergency;
		}
	}
}
