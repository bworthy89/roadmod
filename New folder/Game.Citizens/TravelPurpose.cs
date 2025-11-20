using Colossal.Serialization.Entities;
using Game.Economy;
using Unity.Entities;

namespace Game.Citizens;

public struct TravelPurpose : IComponentData, IQueryTypeParameter, ISerializable
{
	public Purpose m_Purpose;

	public int m_Data;

	public Resource m_Resource;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		Purpose purpose = m_Purpose;
		writer.Write((byte)purpose);
		int data = m_Data;
		writer.Write(data);
		sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Resource);
		writer.Write(value);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		reader.Read(out byte value);
		ref int data = ref m_Data;
		reader.Read(out data);
		reader.Read(out sbyte value2);
		m_Purpose = (Purpose)value;
		m_Resource = EconomyUtils.GetResource(value2);
	}
}
