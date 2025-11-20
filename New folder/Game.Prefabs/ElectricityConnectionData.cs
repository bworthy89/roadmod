using Colossal.Serialization.Entities;
using Game.Net;
using Unity.Entities;

namespace Game.Prefabs;

public struct ElectricityConnectionData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Capacity;

	public FlowDirection m_Direction;

	public ElectricityConnection.Voltage m_Voltage;

	public CompositionFlags m_CompositionAll;

	public CompositionFlags m_CompositionAny;

	public CompositionFlags m_CompositionNone;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int capacity = m_Capacity;
		writer.Write(capacity);
		FlowDirection direction = m_Direction;
		writer.Write((byte)direction);
		ElectricityConnection.Voltage voltage = m_Voltage;
		writer.Write((byte)voltage);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int capacity = ref m_Capacity;
		reader.Read(out capacity);
		reader.Read(out byte value);
		reader.Read(out byte value2);
		m_Direction = (FlowDirection)value;
		m_Voltage = (ElectricityConnection.Voltage)value2;
	}
}
