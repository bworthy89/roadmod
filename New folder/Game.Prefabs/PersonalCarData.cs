using Colossal.Serialization.Entities;
using Unity.Entities;

namespace Game.Prefabs;

public struct PersonalCarData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_PassengerCapacity;

	public int m_BaggageCapacity;

	public int m_CostToDrive;

	public int m_Probability;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int passengerCapacity = m_PassengerCapacity;
		writer.Write(passengerCapacity);
		int baggageCapacity = m_BaggageCapacity;
		writer.Write(baggageCapacity);
		int costToDrive = m_CostToDrive;
		writer.Write(costToDrive);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int passengerCapacity = ref m_PassengerCapacity;
		reader.Read(out passengerCapacity);
		ref int baggageCapacity = ref m_BaggageCapacity;
		reader.Read(out baggageCapacity);
		ref int costToDrive = ref m_CostToDrive;
		reader.Read(out costToDrive);
	}
}
