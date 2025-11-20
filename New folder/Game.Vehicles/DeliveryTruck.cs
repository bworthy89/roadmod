using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;

namespace Game.Vehicles;

public struct DeliveryTruck : IComponentData, IQueryTypeParameter, ISerializable
{
	public DeliveryTruckFlags m_State;

	public Resource m_Resource;

	public int m_Amount;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		DeliveryTruckFlags state = m_State;
		writer.Write((uint)state);
		sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Resource);
		writer.Write(value);
		int amount = m_Amount;
		writer.Write(amount);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out uint value);
		reader.Read(out sbyte value2);
		ref int amount = ref m_Amount;
		reader.Read(out amount);
		m_State = (DeliveryTruckFlags)value;
		m_Resource = EconomyUtils.GetResource(value2);
	}
}
