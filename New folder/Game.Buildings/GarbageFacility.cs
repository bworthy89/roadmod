using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Buildings;

public struct GarbageFacility : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_GarbageDeliverRequest;

	public Entity m_GarbageReceiveRequest;

	public Entity m_TargetRequest;

	public GarbageFacilityFlags m_Flags;

	public float m_AcceptGarbagePriority;

	public float m_DeliverGarbagePriority;

	public int m_ProcessingRate;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity garbageDeliverRequest = m_GarbageDeliverRequest;
		writer.Write(garbageDeliverRequest);
		Entity garbageReceiveRequest = m_GarbageReceiveRequest;
		writer.Write(garbageReceiveRequest);
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		GarbageFacilityFlags flags = m_Flags;
		writer.Write((byte)flags);
		float acceptGarbagePriority = m_AcceptGarbagePriority;
		writer.Write(acceptGarbagePriority);
		float deliverGarbagePriority = m_DeliverGarbagePriority;
		writer.Write(deliverGarbagePriority);
		int processingRate = m_ProcessingRate;
		writer.Write(processingRate);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		if (reader.context.version >= Version.transferRequestRefactoring)
		{
			ref Entity garbageDeliverRequest = ref m_GarbageDeliverRequest;
			reader.Read(out garbageDeliverRequest);
			ref Entity garbageReceiveRequest = ref m_GarbageReceiveRequest;
			reader.Read(out garbageReceiveRequest);
		}
		else if (reader.context.version >= Version.garbageFacilityRefactor2)
		{
			reader.Read(out Entity _);
		}
		if (reader.context.version >= Version.reverseServiceRequests2)
		{
			ref Entity targetRequest = ref m_TargetRequest;
			reader.Read(out targetRequest);
		}
		reader.Read(out byte value2);
		if (reader.context.version >= Version.garbageFacilityRefactor)
		{
			ref float acceptGarbagePriority = ref m_AcceptGarbagePriority;
			reader.Read(out acceptGarbagePriority);
			ref float deliverGarbagePriority = ref m_DeliverGarbagePriority;
			reader.Read(out deliverGarbagePriority);
		}
		else
		{
			reader.Read(out int _);
		}
		if (reader.context.version >= Version.garbageProcessing && reader.context.version < Version.powerPlantConsumption)
		{
			reader.Read(out float _);
		}
		if (reader.context.version >= Version.powerPlantConsumption)
		{
			ref int processingRate = ref m_ProcessingRate;
			reader.Read(out processingRate);
		}
		m_Flags = (GarbageFacilityFlags)value2;
	}
}
