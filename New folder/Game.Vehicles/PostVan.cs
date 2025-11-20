using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Vehicles;

public struct PostVan : IComponentData, IQueryTypeParameter, ISerializable
{
	public Entity m_TargetRequest;

	public PostVanFlags m_State;

	public int m_RequestCount;

	public float m_PathElementTime;

	public int m_DeliveringMail;

	public int m_CollectedMail;

	public int m_DeliveryEstimate;

	public int m_CollectEstimate;

	public PostVan(PostVanFlags flags, int requestCount, int deliveringMail)
	{
		m_TargetRequest = Entity.Null;
		m_State = flags;
		m_RequestCount = requestCount;
		m_PathElementTime = 0f;
		m_DeliveringMail = deliveringMail;
		m_CollectedMail = 0;
		m_DeliveryEstimate = 0;
		m_CollectEstimate = 0;
	}

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Entity targetRequest = m_TargetRequest;
		writer.Write(targetRequest);
		PostVanFlags state = m_State;
		writer.Write((uint)state);
		int requestCount = m_RequestCount;
		writer.Write(requestCount);
		float pathElementTime = m_PathElementTime;
		writer.Write(pathElementTime);
		int deliveringMail = m_DeliveringMail;
		writer.Write(deliveringMail);
		int collectedMail = m_CollectedMail;
		writer.Write(collectedMail);
		int deliveryEstimate = m_DeliveryEstimate;
		writer.Write(deliveryEstimate);
		int collectEstimate = m_CollectEstimate;
		writer.Write(collectEstimate);
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
		if (reader.context.version < Version.taxiDispatchCenter)
		{
			reader.Read(out int _);
		}
		ref int deliveringMail = ref m_DeliveringMail;
		reader.Read(out deliveringMail);
		ref int collectedMail = ref m_CollectedMail;
		reader.Read(out collectedMail);
		m_State = (PostVanFlags)value;
		if (reader.context.version >= Version.policeShiftEstimate)
		{
			ref int deliveryEstimate = ref m_DeliveryEstimate;
			reader.Read(out deliveryEstimate);
			ref int collectEstimate = ref m_CollectEstimate;
			reader.Read(out collectEstimate);
		}
	}
}
