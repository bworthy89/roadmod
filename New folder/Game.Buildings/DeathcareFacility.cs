using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct DeathcareFacility : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetRequest;

	public DeathcareFacilityFlags m_Flags;

	public float m_ProcessingState;

	public int m_LongTermStoredCount;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		DeathcareFacilityFlags flags = m_Flags;
		writer.Write((byte)flags);
		float processingState = m_ProcessingState;
		writer.Write(processingState);
		int longTermStoredCount = m_LongTermStoredCount;
		writer.Write(longTermStoredCount);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.reverseServiceRequests2)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
		}
		reader.Read(out byte value);
		ref float processingState = ref m_ProcessingState;
		reader.Read(out processingState);
		ref int longTermStoredCount = ref m_LongTermStoredCount;
		reader.Read(out longTermStoredCount);
		m_Flags = (DeathcareFacilityFlags)value;
	}
}
