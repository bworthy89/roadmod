using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Routes;

public struct WaitingPassengers : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Count;

	public int m_OngoingAccumulation;

	public int m_ConcludedAccumulation;

	public ushort m_SuccessAccumulation;

	public ushort m_AverageWaitingTime;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int count = m_Count;
		writer.Write(count);
		int ongoingAccumulation = m_OngoingAccumulation;
		writer.Write(ongoingAccumulation);
		int concludedAccumulation = m_ConcludedAccumulation;
		writer.Write(concludedAccumulation);
		ushort successAccumulation = m_SuccessAccumulation;
		writer.Write(successAccumulation);
		ushort averageWaitingTime = m_AverageWaitingTime;
		writer.Write(averageWaitingTime);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int count = ref m_Count;
		reader.Read(out count);
		if (reader.context.version >= Version.passengerWaitTimeCost2)
		{
			ref int ongoingAccumulation = ref m_OngoingAccumulation;
			reader.Read(out ongoingAccumulation);
		}
		if (reader.context.version >= Version.passengerWaitTimeCost)
		{
			ref int concludedAccumulation = ref m_ConcludedAccumulation;
			reader.Read(out concludedAccumulation);
			ref ushort successAccumulation = ref m_SuccessAccumulation;
			reader.Read(out successAccumulation);
			ref ushort averageWaitingTime = ref m_AverageWaitingTime;
			reader.Read(out averageWaitingTime);
		}
	}
}
