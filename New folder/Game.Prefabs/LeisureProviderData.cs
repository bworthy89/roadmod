using Colossal.Serialization.Entities;
using Game.Agents;
using Game.Economy;
using Unity.Entities;

namespace Game.Prefabs;

public struct LeisureProviderData : IComponentData, IQueryTypeParameter, ISerializable
{
	public int m_Efficiency;

	public Resource m_Resources;

	public LeisureType m_LeisureType;

	public void Serialize<TWriter>(TWriter writer) where TWriter : IWriter
	{
		int efficiency = m_Efficiency;
		writer.Write(efficiency);
		sbyte value = (sbyte)EconomyUtils.GetResourceIndex(m_Resources);
		writer.Write(value);
		byte value2 = (byte)m_LeisureType;
		writer.Write(value2);
	}

	public void Deserialize<TReader>(TReader reader) where TReader : IReader
	{
		ref int efficiency = ref m_Efficiency;
		reader.Read(out efficiency);
		reader.Read(out sbyte value);
		reader.Read(out byte value2);
		m_Resources = EconomyUtils.GetResource(value);
		m_LeisureType = (LeisureType)value2;
	}
}
