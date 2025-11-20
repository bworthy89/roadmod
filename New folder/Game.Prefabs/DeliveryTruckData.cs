using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;

namespace Game.Prefabs;

public struct DeliveryTruckData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_CargoCapacity;

	public int m_CostToDrive;

	public Resource m_TransportedResources;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Resource transportedResources = m_TransportedResources;
		writer.Write((ulong)transportedResources);
		int cargoCapacity = m_CargoCapacity;
		writer.Write(cargoCapacity);
		int costToDrive = m_CostToDrive;
		writer.Write(costToDrive);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out ulong value);
		ref int cargoCapacity = ref m_CargoCapacity;
		reader.Read(out cargoCapacity);
		ref int costToDrive = ref m_CostToDrive;
		reader.Read(out costToDrive);
		m_TransportedResources = (Resource)value;
	}
}
